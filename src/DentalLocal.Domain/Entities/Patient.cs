using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.ValueObjects;

namespace DentalLocal.Domain.Entities;

public class Patient : Entity, IImmutableRecord
{
    /// <summary>Автоинкрементный номер карты (`card_number`), присваивается СУБД (GENERATED ALWAYS AS IDENTITY).</summary>
    public int CardNumber { get; private set; }

    public string LastName { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public Gender Gender { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public string Phone { get; private set; } = null!;
    public string? PhoneExtra { get; private set; }
    public string? Address { get; private set; }
    public string? Allergies { get; private set; }
    public string? ChronicDiseases { get; private set; }
    public string? Comments { get; private set; }
    public string? DoctorNotes { get; private set; }
    public string? Source { get; private set; }
    public Guid? PhotoFileId { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? LastVisitAt { get; private set; }
    public bool IsArchived { get; private set; }

    private Patient()
    {
    }

    public static Patient Create(
        string lastName,
        string firstName,
        string? middleName,
        Gender gender,
        DateOnly birthDate,
        string phone,
        string? phoneExtra,
        string? address,
        string? allergies,
        string? chronicDiseases,
        string? comments,
        string? doctorNotes,
        string? source = null)
    {
        ValidateName(lastName, nameof(lastName));
        ValidateName(firstName, nameof(firstName));

        var normalizedPhone = PhoneNumber.Create(phone);
        var normalizedPhoneExtra = string.IsNullOrWhiteSpace(phoneExtra) ? null : PhoneNumber.Create(phoneExtra);

        return new Patient
        {
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            Gender = gender,
            BirthDate = birthDate,
            Phone = normalizedPhone.Value,
            PhoneExtra = normalizedPhoneExtra?.Value,
            Address = address,
            Allergies = allergies,
            ChronicDiseases = chronicDiseases,
            Comments = comments,
            DoctorNotes = doctorNotes,
            Source = source,
            RegisteredAt = DateTimeOffset.UtcNow,
            IsArchived = false,
        };
    }

    public void Update(
        string lastName,
        string firstName,
        string? middleName,
        Gender gender,
        DateOnly birthDate,
        string phone,
        string? phoneExtra,
        string? address,
        string? allergies,
        string? chronicDiseases,
        string? comments,
        string? doctorNotes,
        string? source = null)
    {
        ValidateName(lastName, nameof(lastName));
        ValidateName(firstName, nameof(firstName));

        var normalizedPhone = PhoneNumber.Create(phone);
        var normalizedPhoneExtra = string.IsNullOrWhiteSpace(phoneExtra) ? null : PhoneNumber.Create(phoneExtra);

        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        Gender = gender;
        BirthDate = birthDate;
        Phone = normalizedPhone.Value;
        PhoneExtra = normalizedPhoneExtra?.Value;
        Address = address;
        Allergies = allergies;
        ChronicDiseases = chronicDiseases;
        Comments = comments;
        DoctorNotes = doctorNotes;
        Source = source;
        Touch();
    }

    public void UpdateDoctorNotes(string? doctorNotes)
    {
        DoctorNotes = doctorNotes;
        Touch();
    }

    public void SetPhoto(Guid fileId)
    {
        PhotoFileId = fileId;
        Touch();
    }

    public void RegisterVisit(DateTimeOffset visitAt)
    {
        if (LastVisitAt is null || visitAt > LastVisitAt)
            LastVisitAt = visitAt;

        Touch();
    }

    public void Archive()
    {
        IsArchived = true;
        Touch();
    }

    public void Unarchive()
    {
        IsArchived = false;
        Touch();
    }

    public int GetAge(DateOnly asOf)
    {
        var age = asOf.Year - BirthDate.Year;
        if (BirthDate > asOf.AddYears(-age))
            age--;

        return age;
    }

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {FirstName} {MiddleName}";

    public string CardNumberDisplay => $"К-{CardNumber:D4}";

    private static void ValidateName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Поле обязательно для заполнения.", paramName);
    }
}
