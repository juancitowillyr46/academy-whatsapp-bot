using System.Security.Cryptography;
using System.Text;

namespace AcademyWhatsAppBot.Api.Webhook;

/// <summary>
/// Validates Meta's X-Hub-Signature-256 header: "sha256=" + hex(HMAC-SHA256(rawBody, appSecret)).
/// </summary>
public static class SignatureValidator
{
    private const string Prefix = "sha256=";

    public static bool IsValid(byte[] rawBody, string? signatureHeader, string appSecret)
    {
        if (signatureHeader is null || !signatureHeader.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        byte[] received;
        try
        {
            received = Convert.FromHexString(signatureHeader[Prefix.Length..]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), rawBody);

        // Constant-time comparison to avoid timing attacks.
        return CryptographicOperations.FixedTimeEquals(expected, received);
    }
}
