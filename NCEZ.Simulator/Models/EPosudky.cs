
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record MedicalAssessment : EntityBase
{
    [Required] public string PatientId { get; init; } = default!;
    [Required] public string PractitionerId { get; init; } = default!;
    [Required] public string Type { get; init; } = "GeneralFitness"; // type of assessment
    [Required] public string Result { get; init; } = "fit"; // fit|unfit|conditional
    [Required] public DateTimeOffset EffectiveDate { get; init; } = DateTimeOffset.UtcNow;
    public string? DocumentRef { get; init; } // link to TemporaryDocument
}
