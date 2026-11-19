namespace AcademyWhatsAppBot.Api.Data;

public interface IStudentRepository
{
    /// <summary>
    /// Returns the active students whose guardian has the given phone (any format; it is normalized).
    /// </summary>
    Task<IReadOnlyList<Student>> FindActiveByGuardianPhoneAsync(string phone, CancellationToken cancellationToken = default);
}
