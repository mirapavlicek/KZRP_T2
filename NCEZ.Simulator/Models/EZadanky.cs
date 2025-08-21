
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public enum OrderPriority { routine, urgent, stat }
public enum OrderStatus { entered, accepted, in_progress, completed, cancelled }

public sealed record Requisition : EntityBase
{
    [Required] public string PatientId { get; init; } = default!;
    [Required] public string RequestingProviderId { get; init; } = default!;
    [Required] public string Type { get; init; } = default!; // e.g., "Lab", "Imaging"
    public OrderPriority Priority { get; init; } = OrderPriority.routine;
    public OrderStatus Status { get; init; } = OrderStatus.entered;
    public DateTimeOffset OrderedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? Note { get; init; }
}
