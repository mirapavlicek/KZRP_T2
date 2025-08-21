
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/ezadanky")]
[Tags("eŽádanky")]
public sealed class EZadankyController : ControllerBase
{
    private readonly IJsonRepository<Requisition> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public EZadankyController(IJsonRepository<Requisition> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost]
    public async Task<ActionResult<IdResponse>> Create([FromBody] Requisition input, CancellationToken ct)
    {
        var id = _ids.NewId();
        var entity = input with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Requisition>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<Requisition>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();

    [HttpPost("{id}/status")]
    public async Task<ActionResult> UpdateStatus(string id, [FromBody] OrderStatus status, CancellationToken ct)
    {
        var item = await _repo.GetAsync(id, ct);
        if (item is null) return NotFound();
        await _repo.UpsertAsync(id, item with { ModifiedAt = _clock.Now, Status = status }, ct);
        return NoContent();
    }
}
