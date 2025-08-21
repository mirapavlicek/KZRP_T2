
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public abstract record EntityBase
{
    [Required] public string Id { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; init; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Count);

public sealed record IdResponse(string Id);
