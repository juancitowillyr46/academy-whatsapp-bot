using AcademyWhatsAppBot.Api.Data;

namespace AcademyWhatsAppBot.Tests.Unit;

public sealed class JsonStudentRepositoryTests : IDisposable
{
    private readonly string _filePath = Path.GetTempFileName();

    public JsonStudentRepositoryTests()
    {
        File.WriteAllText(_filePath, """
            [
              { "student_id": "A001", "student_name": "Juan Pérez", "guardian_name": "Marta Gómez",
                "guardian_phone": "+57 300 123 4567", "period": "Noviembre 2026", "balance": 120000,
                "due_date": "2026-12-05", "payment_link": "https://example.com/pay/A001", "is_active": true },
              { "student_id": "A002", "student_name": "Ana Pérez", "guardian_name": "Marta Gómez",
                "guardian_phone": "573001234567", "period": "Noviembre 2026", "balance": 0,
                "due_date": "2026-12-05", "payment_link": "https://example.com/pay/A002", "is_active": false }
            ]
            """);
    }

    [Fact]
    public async Task FindsActiveStudentsAndMapsSnakeCaseColumns()
    {
        var repository = new JsonStudentRepository(_filePath);

        var students = await repository.FindActiveByGuardianPhoneAsync("3001234567");

        var student = Assert.Single(students);
        Assert.Equal("A001", student.StudentId);
        Assert.Equal("Marta Gómez", student.GuardianName);
        Assert.Equal(120000m, student.Balance);
        Assert.Equal(new DateOnly(2026, 12, 5), student.DueDate);
    }

    [Fact]
    public async Task UnknownPhone_ReturnsEmpty()
    {
        var repository = new JsonStudentRepository(_filePath);

        Assert.Empty(await repository.FindActiveByGuardianPhoneAsync("573000000000"));
    }

    public void Dispose() => File.Delete(_filePath);
}
