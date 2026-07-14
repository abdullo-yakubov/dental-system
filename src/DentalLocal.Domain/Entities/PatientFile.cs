using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

public class PatientFile : Entity
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public PatientFileType FileType { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }

    /// <summary>Относительный путь: Data/Files/{patientId}/{fileId}{ext}.</summary>
    public string StoredPath { get; private set; } = null!;

    public DateTimeOffset UploadedAt { get; private set; }
    public string? Description { get; private set; }
    public string? Sha256Hash { get; private set; }
    public string? BeforeAfterType { get; private set; } // e.g., "Before", "After"

    public const long MaxFileSizeBytes = 200L * 1024 * 1024;

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".dcm", ".pdf", ".zip",
    };

    private PatientFile()
    {
    }

    public static PatientFile Create(
        Guid patientId,
        PatientFileType fileType,
        string fileName,
        string contentType,
        long sizeBytes,
        string storedPath,
        string? description,
        string? sha256Hash = null,
        string? beforeAfterType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Имя файла обязательно.", nameof(fileName));

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"Недопустимый тип файла: «{extension}».", nameof(fileName));

        if (sizeBytes <= 0 || sizeBytes > MaxFileSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Размер файла превышает допустимый лимит (200 МБ).");

        return new PatientFile
        {
            PatientId = patientId,
            FileType = fileType,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StoredPath = storedPath,
            Description = description,
            Sha256Hash = sha256Hash,
            BeforeAfterType = beforeAfterType,
            UploadedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        Touch();
    }
}
