using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/patient-summary")]
[Tags("Patient Summary (IPS)")]
[Produces("application/json")]
public sealed class PatientSummaryController : ControllerBase
{
    private readonly IJsonRepository<PatientSummary> _repo;
    private readonly IJsonRepository<Patient> _patients;
    private readonly IJsonRepository<FhirResourceEnvelope> _fhirRepo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;
    private readonly CodeSetService _codes;

    public PatientSummaryController(
        IJsonRepository<PatientSummary> repo,
        IJsonRepository<Patient> patients,
        IJsonRepository<FhirResourceEnvelope> fhirRepo,
        IdGenerator ids,
        SystemClock clock,
        CodeSetService codes)
    {
        _repo = repo; _patients = patients; _fhirRepo = fhirRepo; _ids = ids; _clock = clock; _codes = codes;
    }

    // --- CRUD ---

    [HttpPost]
    public async Task<ActionResult<IdResponse>> Create([FromBody] PatientSummary input, CancellationToken ct)
    {
        var err = Validate(input);
        if (err is ObjectResult e) return e;

        var id = _ids.NewId();
        await _repo.UpsertAsync(id, Enrich(input) with { Id = id, CreatedAt = _clock.Now }, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientSummary>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<PatientSummary>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } s ? Ok(s) : NotFound();

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] PatientSummary input, CancellationToken ct)
    {
        if (await _repo.GetAsync(id, ct) is null) return NotFound();
        var err = Validate(input);
        if (err is ObjectResult e) return e;
        await _repo.UpsertAsync(id, Enrich(input) with { Id = id, ModifiedAt = _clock.Now }, ct);
        return NoContent();
    }

    [HttpPost("{id}/finalize")]
    public async Task<ActionResult> FinalizeSummary(string id, CancellationToken ct)
    {
        var s = await _repo.GetAsync(id, ct);
        if (s is null) return NotFound();
        var err = Validate(s);
        if (err is ObjectResult e) return e;
        await _repo.UpsertAsync(id, s with { Status = "final", ModifiedAt = _clock.Now }, ct);
        return NoContent();
    }

    // --- GENERATE synthetic ---

    [HttpPost("generate")]
    public async Task<ActionResult<IdResponse>> Generate([FromQuery] string patientId, [FromQuery] int problems = 1, CancellationToken ct = default)
    {
        var now = _clock.Now;
        var probs = new List<PsProblem>();
        // pick SNOMED
        var snomed = _codes.Search("snomed", q: null, skip: 0, take: Math.Max(1, problems));
        foreach (var e in snomed)
            probs.Add(new PsProblem { Code = e.Code, System = "snomed", Display = e.Display, ClinicalStatus = "active", Primary = probs.Count == 0 });

        var obs = new List<PsObservation>
        {
            new PsObservation { Code="718-7", System="loinc", Display="Hemoglobin [Mass/volume] in Blood", Value=135, Unit="g/L", Effective=now.AddDays(-1) }
        };

        var ps = new PatientSummary
        {
            PatientId = patientId,
            Problems = probs,
            Medications = new() { new PsMedication { Name="Metformin", AtcCode="A10BA02", Dose="500 mg", Route="per os", Frequency="2x denně" } },
            Immunizations = new() { new PsImmunization { VaccineCode="1119349007", System="snomed", Display="COVID-19 vaccine" } },
            Observations = obs,
            Status = "draft",
            Note = "Synthetic IPS sample"
        };

        var id = _ids.NewId();
        await _repo.UpsertAsync(id, ps with { Id = id, CreatedAt = now }, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    // --- FHIR export: Bundle(document) + Composition 60591-5 ---

    [HttpGet("{id}/$ips")]
    public async Task<ActionResult<object>> ToIpsBundle(string id, CancellationToken ct)
    {
        var s = await _repo.GetAsync(id, ct);
        if (s is null) return NotFound();

        var patient = await _patients.GetAsync(s.PatientId, ct);
        var status = string.Equals(s.Status, "final", StringComparison.OrdinalIgnoreCase) ? "final" : "preliminary";

        // Patient
        var patId = $"urn:uuid:{Guid.NewGuid()}";
        var pat = new
        {
            resourceType = "Patient",
            id = patId.Split(':').Last(),
            identifier = new[] { new { system = "urn:cz:rc", value = patient?.Identifier ?? s.PatientId } },
            name = new[] { new { use = "official", family = patient?.FamilyName ?? "Unknown", given = new[] { patient?.GivenName ?? "Unknown" } } },
            gender = patient?.Sex,
            birthDate = patient?.BirthDate?.ToString("yyyy-MM-dd")
        };

        // Resources
        var entries = new List<object>();
        entries.Add(new { fullUrl = patId, resource = pat });

        // Conditions
        var condRefs = new List<string>();
        foreach (var p in s.Problems)
        {
            var idu = $"urn:uuid:{Guid.NewGuid()}";
            entries.Add(new
            {
                fullUrl = idu,
                resource = new
                {
                    resourceType = "Condition",
                    id = idu.Split(':').Last(),
                    clinicalStatus = new { coding = new[] { new { system = "http://terminology.hl7.org/CodeSystem/condition-clinical", code = p.ClinicalStatus } } },
                    code = new { coding = new[] { new { system = SystemUrl(p.System), code = p.Code, display = p.Display } } },
                    subject = new { reference = patId }
                }
            });
            condRefs.Add(idu);
        }

        // Allergies
        var algRefs = new List<string>();
        foreach (var a in s.Allergies ?? Enumerable.Empty<PsAllergy>())
        {
            var idu = $"urn:uuid:{Guid.NewGuid()}";
            entries.Add(new
            {
                fullUrl = idu,
                resource = new
                {
                    resourceType = "AllergyIntolerance",
                    id = idu.Split(':').Last(),
                    verificationStatus = new { coding = new[] { new { system = "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", code = a.VerificationStatus } } },
                    code = new { coding = new[] { new { system = SystemUrl(a.System), code = a.Substance, display = a.Display ?? a.Substance } } },
                    patient = new { reference = patId }
                }
            });
            algRefs.Add(idu);
        }

        // Medications -> MedicationStatement
        var medRefs = new List<string>();
        foreach (var m in s.Medications ?? Enumerable.Empty<PsMedication>())
        {
            var idu = $"urn:uuid:{Guid.NewGuid()}";
            entries.Add(new
            {
                fullUrl = idu,
                resource = new
                {
                    resourceType = "MedicationStatement",
                    id = idu.Split(':').Last(),
                    status = m.Status,
                    medicationCodeableConcept = new
                    {
                        text = m.Name,
                        coding = string.IsNullOrWhiteSpace(m.AtcCode) ? null : new[] { new { system = "http://www.whocc.no/atc", code = m.AtcCode } }
                    },
                    subject = new { reference = patId },
                    dosage = new[]
                    {
                        new
                        {
                            text = $"{m.Dose} {m.Route} {m.Frequency}".Trim()
                        }
                    }
                }
            });
            medRefs.Add(idu);
        }

        // Observations
        var obsRefs = new List<string>();
        foreach (var o in s.Observations ?? Enumerable.Empty<PsObservation>())
        {
            var idu = $"urn:uuid:{Guid.NewGuid()}";
            entries.Add(new
            {
                fullUrl = idu,
                resource = new
                {
                    resourceType = "Observation",
                    id = idu.Split(':').Last(),
                    status = "final",
                    code = new { coding = new[] { new { system = SystemUrl(o.System), code = o.Code, display = o.Display } } },
                    subject = new { reference = patId },
                    effectiveDateTime = o.Effective,
                    valueQuantity = (o.Value is null) ? null : new { value = o.Value, unit = o.Unit, system = "http://unitsofmeasure.org", code = o.Unit }
                }
            });
            obsRefs.Add(idu);
        }

        // Composition 60591-5 Patient summary Document
        var compId = $"urn:uuid:{Guid.NewGuid()}";
        var composition = new
        {
            resourceType = "Composition",
            id = compId.Split(':').Last(),
            status,
            type = new { coding = new[] { new { system = "http://loinc.org", code = "60591-5", display = "Patient summary Document" } } },
            date = _clock.Now,
            title = "International Patient Summary",
            subject = new { reference = patId },
            section = BuildSections(condRefs, algRefs, medRefs, obsRefs)
        };
        entries.Insert(0, new { fullUrl = compId, resource = composition });

        var bundle = new
        {
            resourceType = "Bundle",
            type = "document",
            timestamp = _clock.Now,
            entry = entries.ToArray()
        };
        return Ok(bundle);
    }

    // --- Ingest + validace cizího IPS dokumentu ---

    [HttpPost("ingest")]
    public async Task<ActionResult<IdResponse>> Ingest([FromBody] JsonElement bundle, CancellationToken ct)
    {
        var outcome = ValidateBundle(bundle);
        if (outcome.Issues.Any(i => i.Severity == "error"))
            return UnprocessableEntity(outcome);

        var id = _ids.NewId();
        await _fhirRepo.UpsertAsync(id, new FhirResourceEnvelope
        {
            Id = id, ResourceType = "Bundle", Resource = JsonSerializer.Deserialize<object>(bundle.GetRawText())!, CreatedAt = _clock.Now
        }, ct);
        return Created($"/api/v1/patient-summary/ingest/{id}", new IdResponse(id));
    }

    [HttpGet("ingest/{id}")]
    public async Task<ActionResult<FhirResourceEnvelope>> GetIngested(string id, CancellationToken ct)
        => (await _fhirRepo.GetAsync(id, ct)) is { } env ? Ok(env) : NotFound();

    [HttpPost("validate-bundle")]
    public ActionResult<OperationOutcome> ValidateIncoming([FromBody] JsonElement bundle)
        => Ok(ValidateBundle(bundle));

    // --- helpers ---

    private ObjectResult? Validate(PatientSummary s)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(s.PatientId))
            errors["patientId"] = new[] { "Required" };
        if (s.Problems is null || s.Problems.Count == 0)
            errors["problems"] = new[] { "At least one problem required" };
        else if (!s.Problems.Any(p => p.Primary))
            errors["problems.primary"] = new[] { "One primary problem required" };

        // code checks (best effort)
        foreach (var p in s.Problems ?? Enumerable.Empty<PsProblem>())
            if (!_codes.TryGet(p.System ?? "snomed", p.Code, out _))
                errors[$"problems.{p.Code}"] = new[] { $"Unknown code in {p.System}" };

        foreach (var o in s.Observations ?? Enumerable.Empty<PsObservation>())
            if (!_codes.TryGet(o.System ?? "loinc", o.Code, out _))
                errors[$"observations.{o.Code}"] = new[] { $"Unknown code in {o.System}" };

        if (errors.Count > 0)
            return UnprocessableEntity(new ValidationProblemDetails(errors)
            {
                Type = "https://http.dev/errors/validation",
                Title = "Validation failed"
            });
        return null;
    }

    private PatientSummary Enrich(PatientSummary s)
    {
        var probs = new List<PsProblem>();
        foreach (var p in s.Problems ?? Enumerable.Empty<PsProblem>())
        {
            var sys = string.IsNullOrWhiteSpace(p.System) ? "snomed" : p.System!;
            if (_codes.TryGet(sys, p.Code, out var e) && string.IsNullOrWhiteSpace(p.Display))
                probs.Add(p with { System = sys, Display = e.Display });
            else probs.Add(p with { System = sys });
        }

        var obs = new List<PsObservation>();
        foreach (var o in s.Observations ?? Enumerable.Empty<PsObservation>())
        {
            var sys = string.IsNullOrWhiteSpace(o.System) ? "loinc" : o.System!;
            if (_codes.TryGet(sys, o.Code, out var e) && string.IsNullOrWhiteSpace(o.Display))
                obs.Add(o with { System = sys, Display = e.Display });
            else obs.Add(o with { System = sys });
        }

        return s with { Problems = probs, Observations = obs };
    }

    private static string SystemUrl(string? key) => (key ?? "").ToLowerInvariant() switch
    {
        "snomed" => "http://snomed.info/sct",
        "icd10"  => "http://hl7.org/fhir/sid/icd-10",
        "loinc"  => "http://loinc.org",
        "ucum"   => "http://unitsofmeasure.org",
        "atc"    => "http://www.whocc.no/atc",
        _ => key ?? "unknown"
    };

    private static object[] BuildSections(List<string> condRefs, List<string> algRefs, List<string> medRefs, List<string> obsRefs)
    {
        object Sec(string title, IEnumerable<string> refs) => new
        {
            title,
            entry = refs.Select(r => new { reference = r }).ToArray()
        };

        var list = new List<object>();
        if (condRefs.Count > 0) list.Add(Sec("Problems", condRefs));
        if (algRefs.Count > 0)  list.Add(Sec("Allergies", algRefs));
        if (medRefs.Count > 0)  list.Add(Sec("Medications", medRefs));
        if (obsRefs.Count > 0)  list.Add(Sec("Observations", obsRefs));
        return list.ToArray();
    }

    private static OperationOutcome ValidateBundle(JsonElement doc)
    {
        var issues = new List<OperationOutcomeIssue>();

        try
        {
            if (!doc.TryGetProperty("resourceType", out var rt) || rt.GetString() != "Bundle")
                issues.Add(new("error", "structure", "resourceType != Bundle"));
            if (!doc.TryGetProperty("type", out var bt) || bt.GetString() != "document")
                issues.Add(new("error", "structure", "Bundle.type != document"));

            if (doc.TryGetProperty("entry", out var ent) && ent.ValueKind == JsonValueKind.Array && ent.GetArrayLength() > 0)
            {
                var first = ent[0].GetProperty("resource");
                var frt = first.GetProperty("resourceType").GetString();
                if (frt != "Composition")
                    issues.Add(new("error", "structure", "First entry is not Composition"));

                // Composition.type LOINC 60591-5
                var type = first.GetProperty("type").GetProperty("coding");
                bool ok = false;
                foreach (var c in type.EnumerateArray())
                {
                    var sys = c.GetProperty("system").GetString();
                    var code = c.GetProperty("code").GetString();
                    if (string.Equals(sys, "http://loinc.org", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(code, "60591-5", StringComparison.OrdinalIgnoreCase))
                    { ok = true; break; }
                }
                if (!ok) issues.Add(new("error", "coding", "Composition.type should include LOINC 60591-5"));

                if (!first.TryGetProperty("subject", out _))
                    issues.Add(new("error", "structure", "Composition.subject missing"));
            }
            else
            {
                issues.Add(new("error", "structure", "Bundle.entry missing"));
            }
        }
        catch
        {
            issues.Add(new("error", "json", "Invalid JSON structure"));
        }

        if (issues.Count == 0) issues.Add(new("information", "ok", "Valid IPS document"));
        return new OperationOutcome(issues.ToArray());
    }
}