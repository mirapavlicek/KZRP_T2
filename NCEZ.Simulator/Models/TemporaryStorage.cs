
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record TemporaryDocument : EntityBase
{
    [Required] public string Name { get; init; } = default!;
    [Required] public string ContentType { get; init; } = "application/octet-stream";
    [Required] public long Size { get; init; }
    [Required] public string Sha256 { get; init; } = default!;
    [Required] public string StoragePath { get; init; } = default!;
    public string? UploaderId { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
}
