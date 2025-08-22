using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/rid")]
[Tags("RID/DRID")]
public sealed class RidController : ControllerBase
{
    private readonly RidService _svc;
    private readonly IJsonRepository<RidAllocation> _repo;

    public RidController(RidService svc, IJsonRepository<RidAllocation> repo)
    { _svc = svc; _repo = repo; }

    // DRID – jeden nebo dávka
    [HttpPost("drid")]
    public async Task<ActionResult<object>> NewDrid([FromQuery] int count = 1, CancellationToken ct = default)
    {
        if (count <= 1)
        {
            var one = await _svc.AllocateDridAsync(ct);
            return CreatedAtAction(nameof(Get), new { id = one.Id }, new { id = one.Id, drid = one.Value });
        }
        var batch = await _svc.AllocateDridBatchAsync(count, ct);
        return Ok(batch.Select(x => new { id = x.Id, drid = x.Value }));
    }

    // RID – volitelné (pro simulaci)
    [HttpPost]
    public async Task<ActionResult<object>> NewRid([FromQuery] int count = 1, CancellationToken ct = default)
    {
        if (count <= 1)
        {
            var one = await _svc.AllocateRidAsync(ct);
            return CreatedAtAction(nameof(Get), new { id = one.Id }, new { id = one.Id, rid = one.Value });
        }
        var batch = await _svc.AllocateRidBatchAsync(count, ct);
        return Ok(batch.Select(x => new { id = x.Id, rid = x.Value }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RidAllocation>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } a ? Ok(a) : NotFound();

    [HttpGet("validate")]
    public ActionResult<object> Validate([FromQuery] string value)
    {
        var isDrid = RidService.IsValidDrid(value);
        var isRid = RidService.IsValidRid(value);
        return Ok(new { value, valid = isDrid || isRid, type = isDrid ? "DRID" : (isRid ? "RID" : "unknown") });
    }
}