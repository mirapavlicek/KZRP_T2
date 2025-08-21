
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/journal")]
[Tags("Žurnál činností")]
public sealed class JournalController : ControllerBase
{
    private readonly IJsonRepository<ActivityEntry> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public JournalController(IJsonRepository<ActivityEntry> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost("entries")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] ActivityEntry input, CancellationToken ct)
    {
        var id = _ids.NewId();
        var entity = input with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<ActivityEntry>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("entries/{id}")]
    public async Task<ActionResult<ActivityEntry>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();
}
