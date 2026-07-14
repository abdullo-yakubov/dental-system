using System.Diagnostics;
using System.Net.Sockets;
using DentalLocal.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DentalLocal.Infrastructure.Database;

public class PostgresBootstrapService : IPostgresBootstrapService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private readonly ILogger<PostgresBootstrapService> _logger;
    private string _pgsqlPath = string.Empty;
    private string _pgDataPath = string.Empty;
    private int _port = 5544;
    private string _dbName = "dental";
    private string _username = "dental";
    private string _password = "";

    public PostgresBootstrapService(
        IConfiguration configuration,
        IHostEnvironment env,
        ILogger<PostgresBootstrapService> logger)
    {
        _configuration = configuration;
        _env = env;
        _logger = logger;
        InitializePaths();
    }

    private void InitializePaths()
    {
        var connectionString = _configuration.GetConnectionString("Default") ?? string.Empty;
        var connParams = ParseConnectionString(connectionString);

        if (connParams.TryGetValue("Port", out var portStr) && int.TryParse(portStr, out var port))
        {
            _port = port;
        }
        if (connParams.TryGetValue("Database", out var dbName))
        {
            _dbName = dbName;
        }
        if (connParams.TryGetValue("Username", out var username))
        {
            _username = username;
        }
        else if (connParams.TryGetValue("User Id", out var userId))
        {
            _username = userId;
        }
        if (connParams.TryGetValue("Password", out var pwd))
        {
            _password = pwd;
        }

        // Detect portable directories
        // Target: root/Database/pgsql and root/Database/data
        var rootDir = FindProjectRootDir();
        _pgsqlPath = Path.Combine(rootDir, "Database", "pgsql");
        _pgDataPath = Path.Combine(rootDir, "Database", "data");
    }

    private string FindProjectRootDir()
    {
        var dir = _env.ContentRootPath;
        // Search upwards for solution or a marker folder
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "DentalLocal.slnx")) || Directory.Exists(Path.Combine(dir, "src")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return _env.ContentRootPath;
    }

    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString)) return dict;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var eqIndex = part.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = part[..eqIndex].Trim();
                var val = part[(eqIndex + 1)..].Trim();
                dict[key] = val;
            }
        }
        return dict;
    }

    private void CreateRequiredDirectories()
    {
        try
        {
            var rootDir = FindProjectRootDir();
            string[] dirs = [
                Path.Combine(rootDir, "Database"),
                Path.Combine(rootDir, "Database", "pgsql"),
                Path.Combine(rootDir, "Database", "data"),
                Path.Combine(rootDir, "Backups"),
                Path.Combine(rootDir, "Backups", "daily"),
                Path.Combine(rootDir, "Backups", "pre-update"),
                Path.Combine(rootDir, "Backups", "manual"),
                Path.Combine(rootDir, "Reports"),
                Path.Combine(rootDir, "Logs"),
                Path.Combine(rootDir, "Settings"),
                Path.Combine(rootDir, "Temp"),
                Path.Combine(rootDir, "Cache"),
                Path.Combine(rootDir, "Exports"),
                Path.Combine(rootDir, "Imports")
            ];

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    _logger.LogInformation("Creating directory: {Path}", dir);
                    Directory.CreateDirectory(dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create required directories.");
        }
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        CreateRequiredDirectories();

        _logger.LogInformation("Starting PostgreSQL bootstrap check on port {Port}...", _port);

        // Check if port is already active
        if (IsPortActive("127.0.0.1", _port))
        {
            _logger.LogInformation("PostgreSQL port {Port} is already active. Assuming database process is running.", _port);
            await EnsureDatabaseExistsAsync(cancellationToken);
            return;
        }

        // Check if portable PostgreSQL binaries exist
        var pgctlExe = Path.Combine(_pgsqlPath, "bin", "pg_ctl.exe");
        if (!File.Exists(pgctlExe))
        {
            _logger.LogWarning("Portable PostgreSQL binaries not found at {Path}. Skipping startup. Assuming system PostgreSQL is used.", pgctlExe);
            return;
        }

        // Check data directory initialization
        var pgVersionFile = Path.Combine(_pgDataPath, "PG_VERSION");
        if (!File.Exists(pgVersionFile))
        {
            _logger.LogInformation("Initializing portable PostgreSQL data directory at {DataPath}...", _pgDataPath);
            Directory.CreateDirectory(_pgDataPath);

            var initdbExe = Path.Combine(_pgsqlPath, "bin", "initdb.exe");
            var initdbArgs = $"-D \"{_pgDataPath}\" -U {_username} --auth-local=trust --auth-host=trust -E UTF8";
            
            var initProcessInfo = new ProcessStartInfo
            {
                FileName = initdbExe,
                Arguments = initdbArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var initProcess = Process.Start(initProcessInfo);
            if (initProcess != null)
            {
                var stdout = await initProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderr = await initProcess.StandardError.ReadToEndAsync(cancellationToken);
                await initProcess.WaitForExitAsync(cancellationToken);

                if (initProcess.ExitCode != 0)
                {
                    _logger.LogError("Failed to initialize PostgreSQL database: {Error}", stderr);
                    throw new InvalidOperationException($"initdb failed with exit code {initProcess.ExitCode}: {stderr}");
                }
                _logger.LogInformation("PostgreSQL data directory initialized successfully.");
            }
        }

        // Start pg_ctl
        _logger.LogInformation("Starting portable PostgreSQL server process...");
        var startArgs = $"start -D \"{_pgDataPath}\" -o \"-p {_port}\"";
        var startProcessInfo = new ProcessStartInfo
        {
            FileName = pgctlExe,
            Arguments = startArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var startProcess = Process.Start(startProcessInfo);
        if (startProcess != null)
        {
            var stdout = await startProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await startProcess.StandardError.ReadToEndAsync(cancellationToken);
            await startProcess.WaitForExitAsync(cancellationToken);

            if (startProcess.ExitCode != 0)
            {
                _logger.LogError("Failed to start PostgreSQL server process: {Error}", stderr);
                throw new InvalidOperationException($"pg_ctl start failed with exit code {startProcess.ExitCode}: {stderr}");
            }
        }

        // Wait for port to become active
        _logger.LogInformation("Waiting for PostgreSQL to start listening on port {Port}...", _port);
        var sw = Stopwatch.StartNew();
        var portOpened = false;
        while (sw.ElapsedMilliseconds < 15000)
        {
            if (IsPortActive("127.0.0.1", _port))
            {
                portOpened = true;
                break;
            }
            await Task.Delay(500, cancellationToken);
        }

        if (!portOpened)
        {
            throw new TimeoutException($"PostgreSQL server failed to start on port {_port} within 15 seconds.");
        }

        _logger.LogInformation("PostgreSQL is running and listening on port {Port}.", _port);
        await EnsureDatabaseExistsAsync(cancellationToken);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var pgctlExe = Path.Combine(_pgsqlPath, "bin", "pg_ctl.exe");
        if (!File.Exists(pgctlExe))
        {
            _logger.LogInformation("Portable pg_ctl.exe not found. Skipping graceful shutdown.");
            return;
        }

        _logger.LogInformation("Stopping portable PostgreSQL server process gracefully...");
        var stopArgs = $"stop -D \"{_pgDataPath}\" -m fast";
        var stopProcessInfo = new ProcessStartInfo
        {
            FileName = pgctlExe,
            Arguments = stopArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var stopProcess = Process.Start(stopProcessInfo);
        if (stopProcess != null)
        {
            await stopProcess.WaitForExitAsync(cancellationToken);
            _logger.LogInformation("PostgreSQL server process stopped.");
        }
    }

    private static bool IsPortActive(string host, int port)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var result = tcpClient.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            if (!success) return false;
            tcpClient.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        // Connect to the default 'postgres' database to check if our database exists
        var connectionString = _configuration.GetConnectionString("Default") ?? string.Empty;
        var connParams = ParseConnectionString(connectionString);

        connParams["Database"] = "postgres"; // Connect to postgres template database
        
        var sysConnStrBuilder = new NpgsqlConnectionStringBuilder();
        foreach (var (k, v) in connParams)
        {
            sysConnStrBuilder[k] = v;
        }

        using var conn = new NpgsqlConnection(sysConnStrBuilder.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var dbExists = false;
        using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = @dbName", conn))
        {
            cmd.Parameters.AddWithValue("dbName", _dbName);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            dbExists = result != null;
        }

        if (!dbExists)
        {
            _logger.LogInformation("Creating database '{Database}'...", _dbName);
            using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{_dbName}\" OWNER = \"{_username}\"", conn);
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Database '{Database}' created successfully.", _dbName);
        }
    }
}
