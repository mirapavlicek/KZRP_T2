
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record PortalMessage : EntityBase
{
    [Required] public string Title { get; init; } = default!;
    [Required] public string Body { get; init; } = default!;
    public string? Audience { get; init; } // patient|provider|public
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
}
