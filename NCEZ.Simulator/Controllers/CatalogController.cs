
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/catalog")]
[Tags("Katalog služeb EZ")]
public sealed class CatalogController : ControllerBase
{
    private readonly IJsonRepository<ServiceCatalogItem> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public CatalogController(IJsonRepository<ServiceCatalogItem> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost("services")]
    public async Task<ActionResult<IdResponse>> Create([FromBody] ServiceCatalogItem input, CancellationToken ct)
    {
        var id = _ids.NewId();
        await _repo.UpsertAsync(id, input with { Id = id, CreatedAt = _clock.Now }, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet("services")]
    public async Task<ActionResult<IEnumerable<ServiceCatalogItem>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("services/{id}")]
    public async Task<ActionResult<ServiceCatalogItem>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();
}
