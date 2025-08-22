using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace NCEZ.Simulator.Models;

public abstract record EntityBase
{
    [Required] public string Id { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; init; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Count);

public sealed record IdResponse(string Id);

public static class HashHelper
{
    public static string Sha1Base64(byte[] data) => Convert.ToBase64String(SHA1.HashData(data));
}