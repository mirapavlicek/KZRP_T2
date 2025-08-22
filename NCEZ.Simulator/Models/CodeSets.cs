using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record CodeEntry
{
    [Required] public string System { get; init; } = default!;
    [Required] public string Code { get; init; } = default!;
    [Required] public string Display { get; init; } = default!;
    public string? Version { get; init; }
    public string? Lang { get; init; }
    public string[]? Synonyms { get; init; }
    public string[]? Parents { get; init; }
    public bool? Active { get; init; }
}

public sealed record CodeSystemMeta(string System, string Title, string? DefaultVersion, int Count, DateTimeOffset LoadedAt);

public sealed record ValidateCodesResult(string Code, bool Valid, string? Display, string System);

public sealed record ConceptMapEntry
{
    [Required] public string SourceSystem { get; init; } = default!;
    [Required] public string SourceCode { get; init; } = default!;
    [Required] public string TargetSystem { get; init; } = default!;
    [Required] public string TargetCode { get; init; } = default!;
    public string? Relationship { get; init; } // equivalent|narrower|broader|related
}

public sealed record OperationOutcomeIssue(string Severity, string Code, string Details);
public sealed record OperationOutcome(OperationOutcomeIssue[] Issues);