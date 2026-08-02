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

    // .zip намеренно исключён: без просмотрщика в системе загруженный архив — просто файлопомойка,
    // а не медкарта, и расширяет площадь атаки (архивы можно набить чем угодно).
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".dcm", ".pdf",
    };

    /// <summary>
    /// Проверка сигнатуры файла (magic bytes) вместо доверия одному лишь расширению —
    /// переименованный .exe в .png не должен пройти как изображение.
    /// </summary>
    public static bool HasValidSignature(string extension, ReadOnlySpan<byte> content) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        ".png" => content.Length >= 8
            && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47
            && content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A,
        ".webp" => content.Length >= 12
            && content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F'
            && content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P',
        ".bmp" => content.Length >= 2 && content[0] == (byte)'B' && content[1] == (byte)'M',
        ".pdf" => content.Length >= 4 && content[0] == (byte)'%' && content[1] == (byte)'P' && content[2] == (byte)'D' && content[3] == (byte)'F',
        // DICOM: 128-байтный преамбул, затем магическая строка "DICM".
        ".dcm" => content.Length >= 132
            && content[128] == (byte)'D' && content[129] == (byte)'I' && content[130] == (byte)'C' && content[131] == (byte)'M',
        _ => false,
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
