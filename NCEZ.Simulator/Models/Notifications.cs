
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record NotificationMessage : EntityBase
{
    [Required] public string Type { get; init; } = default!; // e.g., "FHIROperation", "LabResultReady"
    public string? Topic { get; init; }
    public string? CorrelationId { get; init; }
    [Required] public object Payload { get; init; } = default!;
    [Required] public string Status { get; init; } = "new"; // new, delivered, acknowledged
    [Required] public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
