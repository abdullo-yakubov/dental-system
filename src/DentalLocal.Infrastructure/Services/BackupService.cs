using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DentalLocal.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BackupService> _logger;

    private string _pgsqlPath = string.Empty;
    private string _rootDir = string.Empty;
    private int _port = 5544;
    private string _dbName = "dental";
    private string _username = "dental";
    private string _password = "";

    public BackupService(
        IConfiguration configuration,
        IHostEnvironment env,
        IUnitOfWork unitOfWork,
        ILogger<BackupService> logger)
    {
        _configuration = configuration;
        _env = env;
        _unitOfWork = unitOfWork;
        _logger = logger;
        InitializeParams();
    }

    private void InitializeParams()
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

        _rootDir = FindProjectRootDir();
        _pgsqlPath = Path.Combine(_rootDir, "Database", "pgsql");
    }

    private string FindProjectRootDir()
    {
        var dir = _env.ContentRootPath;
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

    public async Task<string> CreateBackupAsync(string pin, string? note = null, CancellationToken cancellationToken = default)
    {
        var trigger = BackupTrigger.Manual;
        if (note != null && note.Contains("auto", StringComparison.OrdinalIgnoreCase))
        {
            trigger = BackupTrigger.DailyAuto;
        }
        else if (note != null && note.Contains("restore", StringComparison.OrdinalIgnoreCase))
        {
            trigger = BackupTrigger.BeforeRestore;
        }

        var folderName = trigger == BackupTrigger.DailyAuto ? "daily" : (trigger == BackupTrigger.BeforeRestore ? "pre-update" : "manual");
        var backupsDir = Path.Combine(_rootDir, "Backups", folderName);
        Directory.CreateDirectory(backupsDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"backup_{timestamp}.backup";
        var relativePath = Path.Combine("Backups", folderName, fileName);
        var absolutePath = Path.Combine(backupsDir, fileName);

        var tempRawFile = Path.Combine(_rootDir, "Temp", $"raw_{Guid.NewGuid()}.tmp");
        Directory.CreateDirectory(Path.GetDirectoryName(tempRawFile)!);

        try
        {
            // 1. Run pg_dump
            var pgDumpExe = Path.Combine(_pgsqlPath, "bin", "pg_dump.exe");
            if (!File.Exists(pgDumpExe))
            {
                throw new FileNotFoundException($"pg_dump.exe not found at {pgDumpExe}");
            }

            var args = $"-h 127.0.0.1 -p {_port} -U {_username} -F c -b -v -f \"{tempRawFile}\" {_dbName}";
            var processInfo = new ProcessStartInfo
            {
                FileName = pgDumpExe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            processInfo.EnvironmentVariables["PGPASSWORD"] = _password;

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start pg_dump process.");
            }

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"pg_dump failed with exit code {process.ExitCode}: {stderr}");
            }

            // 2. Encrypt pg_dump file
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            var encryptionPassword = string.IsNullOrEmpty(pin) ? "dental_auto_backup" : pin;
            using (var deriveBytes = new Rfc2898DeriveBytes(encryptionPassword, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] key = deriveBytes.GetBytes(32);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var encryptor = aes.CreateEncryptor();
                using var fsOut = new FileStream(absolutePath, FileMode.Create, FileAccess.Write);
                
                // Write header (salt + iv)
                await fsOut.WriteAsync(salt, cancellationToken);
                await fsOut.WriteAsync(iv, cancellationToken);

                using var cryptoStream = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);
                using var fsIn = new FileStream(tempRawFile, FileMode.Open, FileAccess.Read);
                await fsIn.CopyToAsync(cryptoStream, cancellationToken);
            }

            // 3. Write SHA256 of the encrypted file
            byte[] fileBytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
            byte[] hashBytes = SHA256.HashData(fileBytes);
            var sha256Hex = Convert.ToHexString(hashBytes).ToLowerInvariant();
            await File.WriteAllTextAsync(absolutePath + ".sha256", sha256Hex, cancellationToken);

            // 4. Save to Database
            var record = BackupRecord.Create(fileName, relativePath, fileBytes.Length, trigger, true, note);
            await _unitOfWork.BackupRecords.AddAsync(record, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database backup '{FileName}' created successfully.", fileName);
            return relativePath;
        }
        finally
        {
            if (File.Exists(tempRawFile))
            {
                File.Delete(tempRawFile);
            }
        }
    }

    public async Task RestoreBackupAsync(string pin, string backupFileName, CancellationToken cancellationToken = default)
    {
        // 1. Locate backup
        var relativePath = backupFileName;
        var absolutePath = Path.IsPathRooted(backupFileName) ? backupFileName : Path.Combine(_rootDir, relativePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Backup file not found: {absolutePath}");
        }

        // 2. Safety Backup
        _logger.LogInformation("Creating safety backup before restoring '{FileName}'...", backupFileName);
        await CreateBackupAsync(pin, $"Safety backup before restoring {Path.GetFileName(backupFileName)}", cancellationToken);

        var tempRawFile = Path.Combine(_rootDir, "Temp", $"decrypted_{Guid.NewGuid()}.tmp");
        Directory.CreateDirectory(Path.GetDirectoryName(tempRawFile)!);

        try
        {
            // 3. Decrypt file
            bool decrypted = false;
            try
            {
                await DecryptFileAsync(absolutePath, tempRawFile, pin, cancellationToken);
                decrypted = true;
            }
            catch (CryptographicException)
            {
                try
                {
                    _logger.LogInformation("Decryption with provided PIN failed. Retrying with auto-backup password fallback...");
                    await DecryptFileAsync(absolutePath, tempRawFile, "dental_auto_backup", cancellationToken);
                    decrypted = true;
                }
                catch
                {
                    throw new CryptographicException("Неверный PIN-код или поврежденный файл резервной копии.");
                }
            }

            // 4. Terminate other connections to 'dental' database and drop/recreate
            _logger.LogInformation("Terminating active connections and dropping database '{Database}'...", _dbName);
            await RecreateDatabaseAsync(cancellationToken);

            // 5. Restore DB using pg_restore
            var pgRestoreExe = Path.Combine(_pgsqlPath, "bin", "pg_restore.exe");
            if (!File.Exists(pgRestoreExe))
            {
                throw new FileNotFoundException($"pg_restore.exe not found at {pgRestoreExe}");
            }

            _logger.LogInformation("Running pg_restore onto database '{Database}'...", _dbName);
            var args = $"-h 127.0.0.1 -p {_port} -U {_username} -d {_dbName} -v \"{tempRawFile}\"";
            var processInfo = new ProcessStartInfo
            {
                FileName = pgRestoreExe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            processInfo.EnvironmentVariables["PGPASSWORD"] = _password;

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start pg_restore process.");
            }

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Note: pg_restore might exit with code 1 for minor warnings, but we check standard error or expect 0
            if (process.ExitCode != 0 && process.ExitCode != 1)
            {
                throw new InvalidOperationException($"pg_restore failed with exit code {process.ExitCode}: {stderr}");
            }

            _logger.LogInformation("Database restore completed successfully.");
        }
        finally
        {
            if (File.Exists(tempRawFile))
            {
                File.Delete(tempRawFile);
            }
        }
    }

    public async Task<bool> ValidateBackupIntegrityAsync(string backupFileName, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.IsPathRooted(backupFileName) ? backupFileName : Path.Combine(_rootDir, backupFileName);
        if (!File.Exists(absolutePath)) return false;

        var sha256File = absolutePath + ".sha256";
        if (!File.Exists(sha256File)) return false;

        try
        {
            var expectedHash = (await File.ReadAllTextAsync(sha256File, cancellationToken)).Trim().ToLowerInvariant();
            byte[] fileBytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
            byte[] hashBytes = SHA256.HashData(fileBytes);
            var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return string.Equals(expectedHash, actualHash, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private async Task DecryptFileAsync(string srcPath, string destPath, string password, CancellationToken cancellationToken)
    {
        using var fsIn = new FileStream(srcPath, FileMode.Open, FileAccess.Read);
        byte[] salt = new byte[16];
        byte[] iv = new byte[16];

        int bytesRead = await fsIn.ReadAsync(salt.AsMemory(0, 16), cancellationToken);
        if (bytesRead < 16) throw new InvalidDataException("Invalid backup file header (salt too short).");

        bytesRead = await fsIn.ReadAsync(iv.AsMemory(0, 16), cancellationToken);
        if (bytesRead < 16) throw new InvalidDataException("Invalid backup file header (IV too short).");

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        byte[] key = deriveBytes.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);
        using var fsOut = new FileStream(destPath, FileMode.Create, FileAccess.Write);
        await cryptoStream.CopyToAsync(fsOut, cancellationToken);
    }

    private async Task RecreateDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("Default") ?? string.Empty;
        var connParams = ParseConnectionString(connectionString);
        connParams["Database"] = "postgres";

        var sysConnStrBuilder = new NpgsqlConnectionStringBuilder();
        foreach (var (k, v) in connParams)
        {
            sysConnStrBuilder[k] = v;
        }

        using var conn = new NpgsqlConnection(sysConnStrBuilder.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        // Terminate other active sessions
        var terminateQuery = $@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = @dbName
              AND pid <> pg_backend_pid();";

        using (var cmd = new NpgsqlCommand(terminateQuery, conn))
        {
            cmd.Parameters.AddWithValue("dbName", _dbName);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Drop
        using (var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_dbName}\"", conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Create
        using (var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_dbName}\" OWNER = \"{_username}\"", conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
