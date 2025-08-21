
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record ActivityEntry : EntityBase
{
    [Required] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    [Required] public string ActorId { get; init; } = default!;
    [Required] public string Action { get; init; } = default!; // e.g., "Create", "Update", "Search"
    [Required] public string ResourceType { get; init; } = default!;
    public string? ResourceId { get; init; }
    [Required] public string Outcome { get; init; } = "success"; // success|failure
    public string? Details { get; init; }
}
