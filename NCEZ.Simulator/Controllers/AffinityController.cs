
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/affinity")]
[Tags("Afinitní domény (XDS.b simulace)")]
public sealed class AffinityController : ControllerBase
{
    private readonly IJsonRepository<DocumentEntry> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public AffinityController(IJsonRepository<DocumentEntry> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost("provide-and-register")]
    public async Task<ActionResult<IdResponse>> ProvideAndRegister([FromBody] DocumentEntry input, CancellationToken ct)
    {
        var id = _ids.NewId();
        var entity = input with { Id = id, DocumentUniqueId = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(GetEntry), new { id }, new IdResponse(id));
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<DocumentEntry>>> List(int skip = 0, int take = 100, string? patientId = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(patientId))
        {
            var filtered = await _repo.SearchAsync(e => string.Equals(e.PatientId, patientId, StringComparison.OrdinalIgnoreCase), skip, take, ct);
            return Ok(filtered);
        }
        return Ok(await _repo.ListAsync(skip, take, ct));
    }

    [HttpGet("entries/{id}")]
    public async Task<ActionResult<DocumentEntry>> GetEntry(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } e ? Ok(e) : NotFound();
}
