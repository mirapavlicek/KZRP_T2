
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record AuthorizationGrant : EntityBase
{
    [Required] public string SubjectType { get; init; } = default!; // Person|Organization
    [Required] public string SubjectId { get; init; } = default!;
    [Required] public string Role { get; init; } = default!; // e.g., "Practitioner", "ProviderAdmin"
    [Required] public string Scope { get; init; } = default!; // e.g., "Patient.read", "Document.write"
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    [Required] public DateTimeOffset ValidFrom { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ValidTo { get; init; }
    public string? GrantedBy { get; init; }
    public string? Note { get; init; }
}
