using System.Text.RegularExpressions;

namespace DentalLocal.Domain.ValueObjects;

/// <summary>
/// Нормализованный номер телефона в формате +992XXXXXXXXX.
/// </summary>
public sealed partial class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Номер телефона не может быть пустым.", nameof(raw));

        var digits = DigitsRegex().Replace(raw, string.Empty);

        if (digits.StartsWith('8') && digits.Length == 10)
            digits = "992" + digits[1..];

        if (!digits.StartsWith("992") && digits.Length == 9)
            digits = "992" + digits;

        if (digits.Length is < 9 or > 15)
            throw new ArgumentException($"Некорректный номер телефона: «{raw}».", nameof(raw));

        return new PhoneNumber("+" + digits);
    }

    public override string ToString() => Value;

    public bool Equals(PhoneNumber? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as PhoneNumber);

    public override int GetHashCode() => Value.GetHashCode();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex DigitsRegex();
}
