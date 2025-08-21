
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

// Simplified XDS.b-like objects for simulator
public sealed record DocumentEntry : EntityBase
{
    [Required] public string PatientId { get; init; } = default!;
    [Required] public string ClassCode { get; init; } = "34133-9"; // e.g., LOINC class code for summary
    public string? TypeCode { get; init; }
    public string? FormatCode { get; init; } // e.g., "urn:ihe:pcc:handp:2008"
    public string? RepositoryUniqueId { get; init; }
    public string? DocumentUniqueId { get; init; }
    public string? Title { get; init; }
    public DateTimeOffset ServiceStart { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ServiceEnd { get; init; } = DateTimeOffset.UtcNow;
    public string? DocumentRef { get; init; } // link to TemporaryDocument
}
