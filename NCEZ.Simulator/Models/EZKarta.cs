
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record PatientCard : EntityBase
{
    [Required] public string PatientId { get; init; } = default!;
    [Required] public string GivenName { get; init; } = default!;
    [Required] public string FamilyName { get; init; } = default!;
    public DateTime? BirthDate { get; init; }
    public string? Sex { get; init; } // male|female|other|unknown
    public string? Summary { get; init; }
    public string[] Allergies { get; init; } = Array.Empty<string>();
    public string[] ActiveProblems { get; init; } = Array.Empty<string>(); // ICD-10/SNOMED codes
}
