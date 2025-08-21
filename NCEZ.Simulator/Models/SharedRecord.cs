
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

// Minimal FHIR container: store arbitrary resource JSON with at least resourceType and id.
public sealed record FhirResourceEnvelope : EntityBase
{
    [Required] public string ResourceType { get; init; } = default!;
    [Required] public object Resource { get; init; } = default!;
}
