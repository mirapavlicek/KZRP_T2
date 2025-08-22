using System.ComponentModel.DataAnnotations;

namespace NCEZ.Simulator.Models;

public sealed record RidAllocation : EntityBase
{
    /// <summary>DRID nebo RID</summary>
    [Required] public string Type { get; init; } = "DRID";
    /// <summary>Hodnota identifikátoru</summary>
    [Required] public string Value { get; init; } = default!;
    /// <summary>allocated|promoted|released</summary>
    public string Status { get; init; } = "allocated";
    /// <summary>Volitelná vazba na pacienta v simulátoru</summary>
    public string? LinkedPatientId { get; init; }
}