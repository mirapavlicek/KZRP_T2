
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/fhir")]
[Tags("Sdílený zdravotní záznam (FHIR R4)")]
public sealed class FhirController : ControllerBase
{
    private readonly IJsonRepository<FhirResourceEnvelope> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public FhirController(IJsonRepository<FhirResourceEnvelope> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    // Create or update a FHIR resource
    [HttpPost("{resourceType}")]
    public async Task<ActionResult<IdResponse>> Create(string resourceType, [FromBody] object resource, CancellationToken ct)
    {
        // resource must have resourceType and optionally id
        var id = _ids.NewId();
        var env = new FhirResourceEnvelope { Id = id, ResourceType = resourceType, Resource = resource, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, env, ct);
        return Created($"/api/v1/fhir/{resourceType}/{id}", new IdResponse(id));
    }

    [HttpGet("{resourceType}")]
    public async Task<ActionResult<IEnumerable<FhirResourceEnvelope>>> List(string resourceType, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        var items = await _repo.SearchAsync(x => string.Equals(x.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase), skip, take, ct);
        return Ok(items);
    }

    [HttpGet("{resourceType}/{id}")]
    public async Task<ActionResult<FhirResourceEnvelope>> Get(string resourceType, string id, CancellationToken ct)
    {
        var item = await _repo.GetAsync(id, ct);
        if (item is null || !string.Equals(item.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase)) return NotFound();
        return Ok(item);
    }
}
