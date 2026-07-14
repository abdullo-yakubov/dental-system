using System.Diagnostics;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalLocal.Infrastructure.BackgroundServices;

public class DailyMaintenanceBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyMaintenanceBackgroundService> _logger;

    private DateTime _lastDailyBackupTime = DateTime.MinValue;
    private DateTime _lastFileIntegrityCheckTime = DateTime.MinValue;
    private DateTime _lastLowStockCheckTime = DateTime.MinValue;

    public DailyMaintenanceBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DailyMaintenanceBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Maintenance Background Service is starting.");

        // Wait a few seconds on startup to let migrations run and ports bind
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Run startup check for backup
        try
        {
            await RunStartupBackupCheckAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during startup backup check.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // 1. Daily auto backup (every 24 hours)
                if (now - _lastDailyBackupTime >= TimeSpan.FromHours(24))
                {
                    await RunDailyBackupAsync(stoppingToken);
                    _lastDailyBackupTime = now;
                }

                // 2. File integrity check (every 12 hours)
                if (now - _lastFileIntegrityCheckTime >= TimeSpan.FromHours(12))
                {
                    await RunFileIntegrityCheckAsync(stoppingToken);
                    _lastFileIntegrityCheckTime = now;
                }

                // 3. Low stock check (every hour)
                if (now - _lastLowStockCheckTime >= TimeSpan.FromHours(1))
                {
                    await RunLowStockCheckAsync(stoppingToken);
                    _lastLowStockCheckTime = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background maintenance cycle.");
            }

            // Sleep for 5 minutes before checking again
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Daily Maintenance Background Service is stopping.");
    }

    private async Task RunStartupBackupCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

        _logger.LogInformation("Performing startup backup check...");
        var latestBackup = await unitOfWork.BackupRecords.GetLatestAsync(cancellationToken);

        if (latestBackup == null || DateTimeOffset.UtcNow - latestBackup.CreatedAt >= TimeSpan.FromHours(24))
        {
            _logger.LogInformation("No recent backups found (or last backup is older than 24 hours). Starting silent background auto-backup...");
            try
            {
                await backupService.CreateBackupAsync(string.Empty, "Silent auto-backup on startup", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run startup auto-backup.");
            }
        }
        else
        {
            _logger.LogInformation("Database has a recent backup created at {Date}. Skipping startup backup.", latestBackup.CreatedAt);
        }
    }

    private async Task RunDailyBackupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

        _logger.LogInformation("Starting scheduled daily automatic backup...");
        try
        {
            await backupService.CreateBackupAsync(string.Empty, "Scheduled daily auto-backup", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run scheduled daily backup.");
        }
    }

    private async Task RunFileIntegrityCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        _logger.LogInformation("Starting scheduled patient files integrity check...");

        // Resolve absolute root path
        var rootDir = Directory.GetCurrentDirectory();
        // Fallback checks
        var filesDir = Path.Combine(rootDir, "Data", "Files");
        if (!Directory.Exists(filesDir))
        {
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            filesDir = Path.Combine(env.ContentRootPath, "Data", "Files");
        }

        try
        {
            var files = await unitOfWork.PatientFiles.GetAllAsync(cancellationToken);

            var missingCount = 0;
            var sizeMismatchCount = 0;

            foreach (var file in files)
            {
                var filePath = Path.Combine(filesDir, file.StoredPath);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File Integrity Check Failed: Patient file missing on disk. ID: {Id}, Name: {Name}, Expected Path: {Path}", file.Id, file.FileName, filePath);
                    missingCount++;
                }
                else
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length != file.SizeBytes)
                    {
                        _logger.LogWarning("File Integrity Check Failed: Patient file size mismatch. ID: {Id}, Name: {Name}, DB Size: {DbSize} bytes, Disk Size: {DiskSize} bytes", file.Id, file.FileName, file.SizeBytes, fileInfo.Length);
                        sizeMismatchCount++;
                    }
                }
            }

            if (missingCount == 0 && sizeMismatchCount == 0)
            {
                _logger.LogInformation("Patient files integrity check completed successfully. Checked {Count} files. All OK.", files.Count);
            }
            else
            {
                _logger.LogWarning("Patient files integrity check completed with errors. Missing files: {Missing}, Mismatching sizes: {Mismatches}.", missingCount, sizeMismatchCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run patient files integrity check.");
        }
    }

    private async Task RunLowStockCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _logger.LogInformation("Starting scheduled low stock check...");
        try
        {
            var lowStockMaterials = await unitOfWork.Materials.GetLowStockAsync(cancellationToken);
            if (lowStockMaterials.Any())
            {
                _logger.LogWarning("Low Stock Warning: The following materials are below their minimum threshold:");
                foreach (var material in lowStockMaterials)
                {
                    _logger.LogWarning("- {Name}: Current Stock: {Quantity} {Unit}, Min Threshold: {Min}", material.Name, material.QuantityInStock, material.Unit, material.MinStockThreshold);
                }
            }
            else
            {
                _logger.LogInformation("Low stock check completed. No materials below threshold.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run low stock check.");
        }
    }
}
