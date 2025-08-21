
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/authorization")]
[Tags("Registr oprávnění")]
public sealed class AuthorizationController : ControllerBase
{
    private readonly IJsonRepository<AuthorizationGrant> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public AuthorizationController(IJsonRepository<AuthorizationGrant> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost("grants")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] AuthorizationGrant input, CancellationToken ct)
    {
        var id = _ids.NewId();
        var entity = input with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet("grants")]
    public async Task<ActionResult<IEnumerable<AuthorizationGrant>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("grants/{id}")]
    public async Task<ActionResult<AuthorizationGrant>> Get(string id, CancellationToken ct)
    {
        var item = await _repo.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("grants/{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] AuthorizationGrant input, CancellationToken ct)
    {
        var existing = await _repo.GetAsync(id, ct);
        if (existing is null) return NotFound();
        await _repo.UpsertAsync(id, input with { Id = id, ModifiedAt = _clock.Now }, ct);
        return NoContent();
    }

    [HttpDelete("grants/{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
        => await _repo.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
