
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/eposudky")]
[Tags("ePosudky")]
public sealed class EPosudkyController : ControllerBase
{
    private readonly IJsonRepository<MedicalAssessment> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public EPosudkyController(IJsonRepository<MedicalAssessment> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    [HttpPost]
    public async Task<ActionResult<IdResponse>> Create([FromBody] MedicalAssessment input, CancellationToken ct)
    {
        var id = _ids.NewId();
        var entity = input with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicalAssessment>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalAssessment>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();
}
