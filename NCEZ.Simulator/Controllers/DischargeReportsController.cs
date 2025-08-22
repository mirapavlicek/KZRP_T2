using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers
{
    [ApiController]
    [Route("api/v1/discharge-reports")]
    public sealed class DischargeReportsController : ControllerBase
    {
        private readonly IJsonRepository<DischargeReport> _repo;
        private readonly IdGenerator _ids;
        private readonly SystemClock _clock;
        private readonly HdrValidator _validator;

        public DischargeReportsController(
            IJsonRepository<DischargeReport> repo,
            IdGenerator ids,
            SystemClock clock,
            HdrValidator validator)
        {
            _repo = repo;
            _ids = ids;
            _clock = clock;
            _validator = validator;
        }

        [HttpPost("fhir")]
        [Consumes("application/fhir+json", "application/json")]
        public async Task<ActionResult<IdResponse>> CreateFhir([FromBody] JsonDocument bundle, CancellationToken ct)
        {
            var errors = _validator.Validate(bundle);
            if (errors.Count > 0)
            {
                var vpd = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["bundle"] = errors.Select(e => $"{e.Code}: {e.Message} {(e.Path ?? "")}".Trim()).ToArray()
                })
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "FHIR validation failed",
                    Type = "https://http.dev/errors/validation"
                };
                return UnprocessableEntity(vpd);
            }

            var id = _ids.NewId();
            var dr = new DischargeReport(TryGetPatientId(bundle.RootElement), _clock.Now, null)
            {
                Id = id,
                Format = "application/fhir+json",
                Raw = bundle.RootElement.GetRawText()
            };

            await _repo.UpsertAsync(id, dr, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new IdResponse(id));
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<DischargeReport>>> List([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
            => Ok(await _repo.ListAsync(skip, take, ct));

        [HttpGet("{id}")]
        public async Task<ActionResult<DischargeReport>> GetById(string id, CancellationToken ct)
        {
            var item = await _repo.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("tests/generate")]
        public async Task<ActionResult<IdResponse>> Generate([FromQuery] string variant = "minimal", CancellationToken ct = default)
        {
            var json = variant.Equals("minimal", StringComparison.OrdinalIgnoreCase) ? MinimalBundle() : FullBundle();
            using var doc = JsonDocument.Parse(json);
            var errors = _validator.Validate(doc);
            if (errors.Count > 0)
            {
                var vpd = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["bundle"] = errors.Select(e => $"{e.Code}: {e.Message} {(e.Path ?? "")}".Trim()).ToArray()
                })
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Generated FHIR failed validation",
                    Type = "https://http.dev/errors/validation"
                };
                return UnprocessableEntity(vpd);
            }

            var id = _ids.NewId();
            var dr = new DischargeReport(null, _clock.Now, variant)
            {
                Id = id,
                Format = "application/fhir+json",
                Raw = json
            };

            await _repo.UpsertAsync(id, dr, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new IdResponse(id));
        }

        private static string? TryGetPatientId(JsonElement root)
        {
            if (!root.TryGetProperty("entry", out var entry) || entry.ValueKind != JsonValueKind.Array) return null;

            foreach (var e in entry.EnumerateArray())
            {
                if (!e.TryGetProperty("resource", out var res) || res.ValueKind != JsonValueKind.Object) continue;
                if (!res.TryGetProperty("resourceType", out var rt) || rt.ValueKind != JsonValueKind.String) continue;
                if (rt.GetString() != "Patient") continue;
                if (res.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
                    return idEl.GetString();
            }
            return null;
        }

        private static string MinimalBundle() => """
        {
          "resourceType": "Bundle",
          "type": "document",
          "entry": [
            {
              "resource": {
                "resourceType": "Composition",
                "status": "final",
                "type": { "coding": [ { "system": "http://loinc.org", "code": "18842-5", "display": "Discharge summary" } ] },
                "subject": { "reference": "Patient/example" },
                "date": "2025-01-01T12:00:00Z",
                "title": "Propouštěcí zpráva"
              }
            },
            {
              "resource": { "resourceType": "Patient", "id": "example" }
            }
          ]
        }
        """;

        private static string FullBundle() => MinimalBundle();
    }
}