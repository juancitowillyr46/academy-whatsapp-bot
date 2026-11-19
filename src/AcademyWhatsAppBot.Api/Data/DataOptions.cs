using System.ComponentModel.DataAnnotations;

namespace AcademyWhatsAppBot.Api.Data;

public sealed class DataOptions
{
    public const string SectionName = "Data";

    /// <summary>Path to the students JSON file, relative to the API project folder (content root).</summary>
    [Required]
    public string FilePath { get; init; } = string.Empty;
}
