using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using DentalLocal.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DentalLocal.Infrastructure.Services;

public class BackupService : IBackupService
{
    // Формат файла бэкапа v2: magic(4)="DLB2" + version(1)=2 + salt(16) + nonce(12) + ciphertext(N) + tag(16),
    // AES-256-GCM (аутентифицированное шифрование), ключ через PBKDF2-SHA256 600 000 итераций.
    // Старые бэкапы (без magic-заголовка) читаются по legacy-формату: salt(16)+iv(16)+AES-256-CBC,
    // PBKDF2 10 000 итераций — только для обратной совместимости при восстановлении, новые бэкапы
    // в этом формате больше не создаются.
    private static readonly byte[] FormatMagic = Encoding.ASCII.GetBytes("DLB2");
    private const byte FormatVersion = 2;
    private const int Pbkdf2Iterations = 600_000;
    private const string LegacyAutoBackupPassword = "dental_auto_backup";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMachineSecretProvider _machineSecretProvider;
    private readonly IDatabaseMigrator _databaseMigrator;
    private readonly IMemoryCache _memoryCache;
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
        IMachineSecretProvider machineSecretProvider,
        IDatabaseMigrator databaseMigrator,
        IMemoryCache memoryCache,
        ILogger<BackupService> logger)
    {
        _configuration = configuration;
        _env = env;
        _unitOfWork = unitOfWork;
        _machineSecretProvider = machineSecretProvider;
        _databaseMigrator = databaseMigrator;
        _memoryCache = memoryCache;
        _logger = logger;
        InitializeParams();
    }

    /// <summary>
    /// Пароль для шифрования/расшифровки автоматических (не PIN-защищённых) бэкапов — берётся
    /// из машинного секрета (DPAPI, привязан к устройству), а не из строки в исходном коде.
    /// </summary>
    private string GetAutoBackupPassword() => Convert.ToBase64String(_machineSecretProvider.GetOrCreateAutoBackupSecret());

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

            // 2. Encrypt pg_dump file (AES-256-GCM, PBKDF2-SHA256 600k итераций; ключ для
            // автобэкапов — из машинного секрета, а не из строки в исходном коде)
            var encryptionPassword = string.IsNullOrEmpty(pin) ? GetAutoBackupPassword() : pin;
            byte[] plaintext = await File.ReadAllBytesAsync(tempRawFile, cancellationToken);
            byte[] encrypted = EncryptBytes(plaintext, encryptionPassword);
            await File.WriteAllBytesAsync(absolutePath, encrypted, cancellationToken);

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
            // 3. Decrypt file: сначала пробуем PIN пользователя (ручные бэкапы), затем машинный
            // секрет (автобэкапы). Формат файла определяется по magic-заголовку — новые (AES-GCM)
            // и старые (AES-CBC, до перехода на AES-GCM) бэкапы поддерживаются одновременно.
            byte[] fileBytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
            var isNewFormat = fileBytes.Length >= FormatMagic.Length && fileBytes.AsSpan(0, FormatMagic.Length).SequenceEqual(FormatMagic);

            byte[]? plaintext = null;
            foreach (var candidatePassword in new[] { pin, isNewFormat ? GetAutoBackupPassword() : LegacyAutoBackupPassword })
            {
                try
                {
                    plaintext = isNewFormat
                        ? DecryptBytesNewFormat(fileBytes, candidatePassword)
                        : DecryptBytesLegacyFormat(fileBytes, candidatePassword);
                    break;
                }
                catch (CryptographicException)
                {
                    // пробуем следующего кандидата
                }
            }

            if (plaintext is null)
            {
                throw new CryptographicException("Неверный PIN-код или поврежденный файл резервной копии.");
            }

            await File.WriteAllBytesAsync(tempRawFile, plaintext, cancellationToken);

            // 4. Освобождаем весь пул соединений Npgsql ДО терминации сессий и DROP DATABASE — иначе
            // пул может выдать соединение к базе, которая уже удалена/пересоздаётся, и следующий
            // запрос упадёт с "database does not exist" или зависнет на явно занятой блокировке.
            NpgsqlConnection.ClearAllPools();

            _logger.LogInformation("Terminating active connections and dropping database '{Database}'...", _dbName);
            await RecreateDatabaseAsync(cancellationToken);

            // 5. Restore DB using pg_restore. --clean --if-exists — на случай, если в целевой БД
            // (созданной заново на шаге 4) уже есть объекты; --if-exists подавляет предупреждения,
            // если чистить нечего.
            var pgRestoreExe = Path.Combine(_pgsqlPath, "bin", "pg_restore.exe");
            if (!File.Exists(pgRestoreExe))
            {
                throw new FileNotFoundException($"pg_restore.exe not found at {pgRestoreExe}");
            }

            _logger.LogInformation("Running pg_restore onto database '{Database}'...", _dbName);
            var args = $"-h 127.0.0.1 -p {_port} -U {_username} -d {_dbName} -v --clean --if-exists \"{tempRawFile}\"";
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

            // 6. Восстановленный дамп мог быть снят на более старой версии схемы — прогоняем миграции
            // поверх него, затем сбрасываем IMemoryCache целиком: иначе в кэше остаются настройки/
            // справочники от базы, которая только что была подменена (см. промпт 7 аудита).
            NpgsqlConnection.ClearAllPools();
            await _databaseMigrator.MigrateAsync(cancellationToken);
            if (_memoryCache is MemoryCache concreteCache)
            {
                concreteCache.Compact(1.0);
            }

            _logger.LogInformation("Database restore completed successfully.");
        }
        finally
        {
            // Пул мог быть заполнен соединениями к промежуточному состоянию БД (до/во время restore) —
            // очищаем его при любом исходе, чтобы последующие запросы не подхватили протухшее соединение.
            NpgsqlConnection.ClearAllPools();

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

    private static byte[] EncryptBytes(byte[] plaintext, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        using var output = new MemoryStream(FormatMagic.Length + 1 + salt.Length + nonce.Length + ciphertext.Length + tag.Length);
        output.Write(FormatMagic);
        output.WriteByte(FormatVersion);
        output.Write(salt);
        output.Write(nonce);
        output.Write(ciphertext);
        output.Write(tag);
        return output.ToArray();
    }

    /// <summary>Расшифровывает бэкап формата v2 (AES-256-GCM). Бросает CryptographicException при неверном пароле/повреждении.</summary>
    private static byte[] DecryptBytesNewFormat(byte[] fileBytes, string password)
    {
        var offset = FormatMagic.Length + 1;
        if (fileBytes.Length < offset + 16 + 12 + 16)
        {
            throw new CryptographicException("Файл резервной копии повреждён или имеет неверный формат.");
        }

        var salt = fileBytes.AsSpan(offset, 16).ToArray();
        offset += 16;
        var nonce = fileBytes.AsSpan(offset, 12).ToArray();
        offset += 12;
        var ciphertextLength = fileBytes.Length - offset - 16;
        var ciphertext = fileBytes.AsSpan(offset, ciphertextLength).ToArray();
        offset += ciphertextLength;
        var tag = fileBytes.AsSpan(offset, 16).ToArray();

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
        var plaintext = new byte[ciphertextLength];
        using var aesGcm = new AesGcm(key, 16);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    /// <summary>
    /// Расшифровывает бэкапы старого формата (salt(16)+iv(16)+AES-256-CBC, PBKDF2 10 000 итераций),
    /// созданные до перехода на AES-GCM. Оставлен только для чтения существующих файлов —
    /// новые бэкапы в этом формате не создаются.
    /// </summary>
    private static byte[] DecryptBytesLegacyFormat(byte[] fileBytes, string password)
    {
        if (fileBytes.Length < 32)
        {
            throw new CryptographicException("Файл резервной копии повреждён или имеет неверный формат.");
        }

        var salt = fileBytes.AsSpan(0, 16).ToArray();
        var iv = fileBytes.AsSpan(16, 16).ToArray();
        var ciphertext = fileBytes.AsSpan(32).ToArray();

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var output = new MemoryStream();
        using (var cryptoStream = new CryptoStream(output, decryptor, CryptoStreamMode.Write))
        {
            cryptoStream.Write(ciphertext);
        }

        return output.ToArray();
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
