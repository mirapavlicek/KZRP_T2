using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record PsProblem
{
    [Required] public string Code { get; init; } = default!;
    public string System { get; init; } = "snomed";
    public string? Display { get; init; }
    public string ClinicalStatus { get; init; } = "active"; // active|resolved|inactive
    public DateTimeOffset? Onset { get; init; }
    public bool Primary { get; init; } = false;
}

public sealed record PsAllergy
{
    [Required] public string Substance { get; init; } = default!;
    public string System { get; init; } = "snomed";
    public string? Display { get; init; }
    public string? Criticality { get; init; } // low|high|unable-to-assess
    public string VerificationStatus { get; init; } = "confirmed";
}

public sealed record PsMedication
{
    [Required] public string Name { get; init; } = default!;
    public string? AtcCode { get; init; }
    public string? Dose { get; init; }
    public string? Route { get; init; }
    public string? Frequency { get; init; }
    public string Status { get; init; } = "active"; // active|completed
}

public sealed record PsImmunization
{
    [Required] public string VaccineCode { get; init; } = default!;
    public string System { get; init; } = "snomed";
    public string? Display { get; init; }
    public DateTimeOffset Date { get; init; } = DateTimeOffset.UtcNow;
    public string Status { get; init; } = "completed";
}

public sealed record PsObservation
{
    [Required] public string Code { get; init; } = default!;   // LOINC
    public string System { get; init; } = "loinc";
    public string? Display { get; init; }
    public decimal? Value { get; init; }
    public string? Unit { get; init; } // UCUM
    public DateTimeOffset Effective { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record PatientSummary : EntityBase
{
    [Required] public string PatientId { get; init; } = default!;
    public string Status { get; init; } = "draft"; // draft|final
    public List<PsProblem> Problems { get; init; } = new();
    public List<PsAllergy>? Allergies { get; init; }
    public List<PsMedication>? Medications { get; init; }
    public List<PsImmunization>? Immunizations { get; init; }
    public List<PsObservation>? Observations { get; init; } // vitals/labs
    public string? Note { get; init; }
}