
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record Patient : EntityBase
{
    [Required] public string Identifier { get; init; } = default!; // e.g., rodné číslo / UUID
    [Required] public string GivenName { get; init; } = default!;
    [Required] public string FamilyName { get; init; } = default!;
    public DateTime? BirthDate { get; init; }
    public string? Sex { get; init; } // male|female|other|unknown
}

public sealed record Practitioner : EntityBase
{
    [Required] public string Identifier { get; init; } = default!; // licence, IČP apod.
    [Required] public string GivenName { get; init; } = default!;
    [Required] public string FamilyName { get; init; } = default!;
    public string? Role { get; init; } // e.g., "MUDr.", "Zubní lékař"
}

public sealed record Provider : EntityBase
{
    [Required] public string Identifier { get; init; } = default!; // IČZ, IČO
    [Required] public string Name { get; init; } = default!;
    public string? Address { get; init; }
}
