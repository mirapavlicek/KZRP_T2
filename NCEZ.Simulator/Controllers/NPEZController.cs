
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/npez")]
[Tags("NPEZ")]
public sealed class NPEZController : ControllerBase
{
    private readonly IJsonRepository<PortalMessage> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public NPEZController(IJsonRepository<PortalMessage> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost("messages")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] PortalMessage input, CancellationToken ct)
    {
        var id = _ids.NewId();
        await _repo.UpsertAsync(id, input with { Id = id, CreatedAt = _clock.Now }, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet("messages")]
    public async Task<ActionResult<IEnumerable<PortalMessage>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("messages/{id}")]
    public async Task<ActionResult<PortalMessage>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();
}
