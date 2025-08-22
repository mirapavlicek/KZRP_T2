using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record DischargeDiagnosis
{
    [Required] public string Code { get; init; } = default!;
    public string System { get; init; } = "icd10";
    public string? Display { get; init; }
    public bool Primary { get; init; } = false;
}

public sealed record DischargeMedication
{
    [Required] public string Name { get; init; } = default!;
    public string? AtcCode { get; init; }
    public string? Dose { get; init; }
    public string? Route { get; init; }
    public string? Frequency { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
}

public sealed record DischargeProcedure
{
    public string? Code { get; init; }
    public string? System { get; init; } = "snomed";
    public string? Display { get; init; }
    public DateTimeOffset? Performed { get; init; }
}

public sealed record DischargeReport : EntityBase
{
    [Required] public string PatientId { get; init; } = default!;
    public string? EncounterId { get; init; }

    [Required] public DateTimeOffset AdmissionDate { get; init; }
    [Required] public DateTimeOffset DischargeDate { get; init; }

    // Povinné části: anamnéza/současná nemoc, průběh, diagnózy.  [oai_citation:3‡Zákony pro lidi](https://www.zakonyprolidi.cz/cs/2024-444?utm_source=chatgpt.com)
    [Required] public string Anamnesis { get; init; } = default!;
    [Required] public string Course { get; init; } = default!;
    [Required] public List<DischargeDiagnosis> Diagnoses { get; init; } = new();

    public List<DischargeProcedure>? Procedures { get; init; }
    public List<DischargeMedication>? Medications { get; init; }
    public string? DischargeInstructions { get; init; }
    public string? AllergiesNote { get; init; }
    public string? FollowUp { get; init; }
    public string? Department { get; init; }
    public string? AttendingPractitionerId { get; init; }

    /// <summary>Odkaz na nahraný PDF soubor v Dočasném úložišti (id dokumentu)</summary>
    public string? PdfDocumentId { get; init; }

    /// <summary>draft|final</summary>
    public string Status { get; init; } = "draft";
}