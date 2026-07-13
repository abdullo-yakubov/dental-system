using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

public class BackupRecord : Entity, IImmutableRecord
{
    public string FileName { get; private set; } = null!;
    public string FilePath { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public BackupTrigger Trigger { get; private set; }
    public bool IsValid { get; private set; }
    public string? Note { get; private set; }

    private BackupRecord()
    {
    }

    public static BackupRecord Create(string fileName, string filePath, long sizeBytes, BackupTrigger trigger, bool isValid, string? note)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Имя файла бэкапа обязательно.", nameof(fileName));

        return new BackupRecord
        {
            FileName = fileName,
            FilePath = filePath,
            SizeBytes = sizeBytes,
            Trigger = trigger,
            IsValid = isValid,
            Note = note,
        };
    }

    public void MarkValidated(bool isValid, string? note)
    {
        IsValid = isValid;
        Note = note;
        Touch();
    }
}
