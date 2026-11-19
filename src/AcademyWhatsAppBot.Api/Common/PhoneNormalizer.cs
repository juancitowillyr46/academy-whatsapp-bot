namespace AcademyWhatsAppBot.Api.Common;

public static class PhoneNormalizer
{
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
