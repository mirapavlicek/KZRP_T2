
using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public enum CertificateStatus { requested, issued, revoked, expired }

public sealed record Certificate : EntityBase
{
    [Required] public string SubjectId { get; init; } = default!;
    [Required] public string CommonName { get; init; } = default!;
    public DateTimeOffset ValidFrom { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ValidTo { get; init; } = DateTimeOffset.UtcNow.AddYears(1);
    public CertificateStatus Status { get; init; } = CertificateStatus.requested;
    public string? Pem { get; init; }
}
