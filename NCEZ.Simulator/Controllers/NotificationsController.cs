
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifikační služby")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IJsonRepository<NotificationMessage> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public NotificationsController(IJsonRepository<NotificationMessage> repo, IdGenerator ids, SystemClock clock)
    {
        _repo = repo; _ids = ids; _clock = clock;
    }

    [HttpPost("publish")]
    public async Task<ActionResult<IdResponse>> Publish([FromBody] NotificationMessage input, CancellationToken ct)
    {
        var id = _ids.NewId();
        var msg = input with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, msg, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new IdResponse(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationMessage>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationMessage>> GetById(string id, CancellationToken ct)
    {
        var msg = await _repo.GetAsync(id, ct);
        return msg is null ? NotFound() : Ok(msg);
    }

    [HttpPost("{id}/ack")]
    public async Task<ActionResult> Ack(string id, CancellationToken ct)
    {
        var msg = await _repo.GetAsync(id, ct);
        if (msg is null) return NotFound();
        await _repo.UpsertAsync(id, msg with { ModifiedAt = _clock.Now, Status = "acknowledged" }, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
        => await _repo.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
