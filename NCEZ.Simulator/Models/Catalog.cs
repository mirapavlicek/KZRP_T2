
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record ServiceCatalogItem : EntityBase
{
    [Required] public string Code { get; init; } = default!;
    [Required] public string Name { get; init; } = default!;
    public string? Version { get; init; }
    public string? EndpointUrl { get; init; }
    public string? Category { get; init; }
}
