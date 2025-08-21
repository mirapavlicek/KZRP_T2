
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/ezca")]
[Tags("EZCA")]
public sealed class EZCAController : ControllerBase
{
    private readonly IJsonRepository<Certificate> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public EZCAController(IJsonRepository<Certificate> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost("certificates")]
   public async Task<ActionResult<IdResponse>> Create([FromBody] Certificate input, CancellationToken ct)
     {
         var id = _ids.NewId();
         var entity = input with { Id = id, CreatedAt = _clock.Now, Status = CertificateStatus.requested };
         await _repo.UpsertAsync(id, entity, ct);
         return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
     }

    [HttpPost("certificates/{id}/issue")]
    public async Task<ActionResult> Issue(string id, CancellationToken ct)
    {
        var cert = await _repo.GetAsync(id, ct);
        if (cert is null) return NotFound();
        var pem = $"-----BEGIN CERTIFICATE-----\\nSIMULATED-{id}\\n-----END CERTIFICATE-----";
        await _repo.UpsertAsync(id, cert with { ModifiedAt = _clock.Now, Status = CertificateStatus.issued, Pem = pem }, ct);
        return NoContent();
    }

    [HttpPost("certificates/{id}/revoke")]
    public async Task<ActionResult> Revoke(string id, CancellationToken ct)
    {
        var cert = await _repo.GetAsync(id, ct);
        if (cert is null) return NotFound();
        await _repo.UpsertAsync(id, cert with { ModifiedAt = _clock.Now, Status = CertificateStatus.revoked }, ct);
        return NoContent();
    }

    [HttpGet("certificates")]
    public async Task<ActionResult<IEnumerable<Certificate>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("certificates/{id}")]
    public async Task<ActionResult<Certificate>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();
}
