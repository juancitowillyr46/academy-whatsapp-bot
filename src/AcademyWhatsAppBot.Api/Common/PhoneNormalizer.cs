namespace AcademyWhatsAppBot.Api.Common;

public static class PhoneNormalizer
{
    private const string ColombiaCountryCode = "57";

    /// <summary>
    /// Normalizes to Meta's format: digits only, with country code (e.g. "+57 300 123-4567" → "573001234567").
    /// A 10-digit Colombian mobile number starting with 3 gets the "57" prefix.
    /// </summary>
    public static string Normalize(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length == 10 && digits[0] == '3'
            ? ColombiaCountryCode + digits
            : digits;
    }

    /// <summary>
    /// Masks a phone number for logs, e.g. 573001234567 → 57300***4567.
    /// </summary>
    public static string Mask(string phone)
    {
        if (phone.Length <= 9)
        {
            return "***";
        }

        return $"{phone[..5]}***{phone[^4..]}";
    }
}
