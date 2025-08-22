using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;
using System.Net;
using System.Text;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/discharge-reports")]
[Tags("Propouštěcí zpráva")]
[Produces("application/json")]
public sealed class DischargeReportsController : ControllerBase
{
    private readonly IJsonRepository<DischargeReport> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;
    private readonly CodeSetService _codes;

    public DischargeReportsController(
        IJsonRepository<DischargeReport> repo,
        IdGenerator ids,
        SystemClock clock,
        CodeSetService codes)
    {
        _repo = repo; _ids = ids; _clock = clock; _codes = codes;
    }

    // CREATE
    [HttpPost]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<IdResponse>> Create([FromBody] DischargeReport input, CancellationToken ct)
    {
        var v = Validate(input);
        if (v is ObjectResult err) return err;

        // doplň zobrazení z číselníků u diagnóz
        var enriched = EnrichDisplay(input);

        var id = _ids.NewId();
        var entity = enriched with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    // GET list
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DischargeReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DischargeReport>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    // GET by id
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DischargeReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DischargeReport>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();

    // UPDATE
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Update(string id, [FromBody] DischargeReport input, CancellationToken ct)
    {
        if (await _repo.GetAsync(id, ct) is null) return NotFound();
        var v = Validate(input);
        if (v is ObjectResult err) return err;

        var entity = EnrichDisplay(input) with { Id = id, ModifiedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return NoContent();
    }

    // FINALIZE (status -> final)
    [HttpPost("{id}/finalize")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> FinalizeReport(string id, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r is null) return NotFound();

        var v = Validate(r);
        if (v is ObjectResult err) return err;

        await _repo.UpsertAsync(id, r with { Status = "final", ModifiedAt = _clock.Now }, ct);
        return NoContent();
    }

    // FHIR export (Composition; LOINC 18842-5 Discharge summary)  [oai_citation:4‡LOINC](https://loinc.org/18842-5?utm_source=chatgpt.com)
    [HttpGet("{id}/$fhir")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ToFhir(string id, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r is null) return NotFound();
        var status = string.Equals(r.Status, "final", StringComparison.OrdinalIgnoreCase) ? "final" : "preliminary";

        object comp = new
        {
            resourceType = "Composition",
            status,
            type = new
            {
                coding = new[] { new { system = "http://loinc.org", code = "18842-5", display = "Discharge summary" } },
                text = "Discharge summary"
            },
            date = r.DischargeDate,
            title = "Propouštěcí zpráva",
            subject = new { reference = $"Patient/{r.PatientId}" },
            encounter = r.EncounterId is null ? null : new { reference = $"Encounter/{r.EncounterId}" },
            author = string.IsNullOrWhiteSpace(r.AttendingPractitionerId) ? null : new[] { new { reference = $"Practitioner/{r.AttendingPractitionerId}" } },
            section = BuildSections(r)
        };

        return Ok(comp);
    }

    // GENERATE synthetic
    [HttpPost("generate")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<IdResponse>> Generate([FromQuery] string patientId, [FromQuery] string level = "L1", CancellationToken ct = default)
    {
        var now = _clock.Now;
        var dx = new List<DischargeDiagnosis>();
        var pick = _codes.Search("icd10", q: null, skip: 0, take: 1);
        var code = pick.FirstOrDefault()?.Code ?? "I10";
        dx.Add(new DischargeDiagnosis { Code = code, System = "icd10", Primary = true });

        var rep = new DischargeReport
        {
            PatientId = patientId,
            AdmissionDate = now.AddDays(-3),
            DischargeDate = now,
            Anamnesis = "Anamnéza: hypertenze, DM2.",
            Course = "Hospitalizace bez komplikací. Stabilizace TK, úprava medikace.",
            Diagnoses = dx,
            Medications = new()
            {
                new DischargeMedication { Name = "Metformin", AtcCode = "A10BA02", Dose = "500 mg", Route = "per os", Frequency = "2x denně" },
                new DischargeMedication { Name = "Perindopril", AtcCode = "C09AA04", Dose = "5 mg", Route = "per os", Frequency = "1x denně" }
            },
            DischargeInstructions = "Kontrola u PL za 2 týdny.",
            FollowUp = "Kontroly TK, glykemie.",
            Status = "draft"
        };

        var id = _ids.NewId();
        await _repo.UpsertAsync(id, EnrichDisplay(rep) with { Id = id, CreatedAt = now }, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    // ===== helpers =====

    private ObjectResult? Validate(DischargeReport r)
    {
        var errors = new Dictionary<string, string[]>();

        if (r.DischargeDate < r.AdmissionDate)
            errors["dischargeDate"] = new[] { "DischargeDate must be >= AdmissionDate" };

        if (string.IsNullOrWhiteSpace(r.Anamnesis))
            errors["anamnesis"] = new[] { "Required" };

        if (string.IsNullOrWhiteSpace(r.Course))
            errors["course"] = new[] { "Required" };

        if (r.Diagnoses is null || r.Diagnoses.Count == 0)
            errors["diagnoses"] = new[] { "At least one diagnosis required" };
        else
        {
            if (!r.Diagnoses.Any(d => d.Primary))
                errors["diagnoses.primary"] = new[] { "One primary diagnosis required" };

            foreach (var d in r.Diagnoses)
            {
                var sys = string.IsNullOrWhiteSpace(d.System) ? "icd10" : d.System!;
                if (!_codes.TryGet(sys, d.Code, out _))
                    errors[$"diagnoses.{d.Code}"] = new[] { $"Unknown code in {sys}" };
            }
        }

        if (errors.Count > 0)
            return UnprocessableEntity(new ValidationProblemDetails(errors)
            {
                Type = "https://http.dev/errors/validation",
                Title = "Validation failed"
            });

        return null;
    }

    private DischargeReport EnrichDisplay(DischargeReport input)
    {
        var dx = new List<DischargeDiagnosis>();
        foreach (var d in input.Diagnoses ?? Enumerable.Empty<DischargeDiagnosis>())
        {
            var sys = string.IsNullOrWhiteSpace(d.System) ? "icd10" : d.System!;
            if (_codes.TryGet(sys, d.Code, out var e) && string.IsNullOrWhiteSpace(d.Display))
                dx.Add(d with { System = sys, Display = e.Display });
            else
                dx.Add(d with { System = sys });
        }
        return input with { Diagnoses = dx };
    }

    private static object[] BuildSections(DischargeReport r)
    {
        static string Html(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
        var sbDx = new StringBuilder();
        foreach (var d in r.Diagnoses ?? Enumerable.Empty<DischargeDiagnosis>())
        {
            var p = d.Primary ? " (hlavní)" : "";
            sbDx.Append($"<li>{Html(d.Code)}{(string.IsNullOrWhiteSpace(d.Display) ? "" : " – " + Html(d.Display!))}{p}</li>");
        }

        var sections = new List<object>
        {
            new { title = "Anamnéza a současná nemoc", text = new { status = "generated", div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>{Html(r.Anamnesis)}</p></div>" } },
            new { title = "Průběh hospitalizace", text = new { status = "generated", div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>{Html(r.Course)}</p></div>" } },
            new { title = "Diagnózy", text = new { status = "generated", div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><ul>{sbDx}</ul></div>" } }
        };

        if (!string.IsNullOrWhiteSpace(r.DischargeInstructions))
            sections.Add(new { title = "Doporučení při propuštění", text = new { status = "generated", div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>{Html(r.DischargeInstructions)}</p></div>" } });

        if (r.Medications is { Count: > 0 })
        {
            var meds = string.Join("", r.Medications.Select(m =>
                $"<li>{Html(m.Name)}{(string.IsNullOrWhiteSpace(m.Dose) ? "" : ", " + Html(m.Dose))}{(string.IsNullOrWhiteSpace(m.Frequency) ? "" : ", " + Html(m.Frequency))}</li>"));
            sections.Add(new { title = "Medikace při propuštění", text = new { status = "generated", div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><ul>{meds}</ul></div>" } });
        }

        if (!string.IsNullOrWhiteSpace(r.FollowUp))
            sections.Add(new { title = "Plán kontrol", text = new { status = "generated", div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>{Html(r.FollowUp)}</p></div>" } });

        return sections.ToArray();
        }
}