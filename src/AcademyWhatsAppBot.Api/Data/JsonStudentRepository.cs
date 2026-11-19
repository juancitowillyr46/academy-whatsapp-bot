using System.Text.Json;

using AcademyWhatsAppBot.Api.Common;

namespace AcademyWhatsAppBot.Api.Data;

/// <summary>
/// Reads students from a JSON file. The file is read on every query so it can be edited during a demo without restarting.
/// </summary>
public sealed class JsonStudentRepository(string filePath) : IStudentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<IReadOnlyList<Student>> FindActiveByGuardianPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = PhoneNormalizer.Normalize(phone);

        await using var stream = File.OpenRead(filePath);
        var students = await JsonSerializer.DeserializeAsync<List<Student>>(stream, JsonOptions, cancellationToken) ?? [];

        return students
            .Where(student => student.IsActive && PhoneNormalizer.Normalize(student.GuardianPhone) == normalizedPhone)
            .ToList();
    }
}
