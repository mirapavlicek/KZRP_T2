using System;

namespace NCEZ.Simulator.Models
{
    public sealed record DischargeReport(string? PatientId, DateTimeOffset ReceivedAt, string? TestVariant) : EntityBase
    {
        public string Format { get; set; } = "application/fhir+json";
        public string Raw { get; set; } = string.Empty;
    }
}