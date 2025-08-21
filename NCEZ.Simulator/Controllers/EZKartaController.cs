
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/ezkarta")]
[Tags("EZKarta")]
public sealed class EZKartaController : ControllerBase
{
    private readonly IJsonRepository<PatientCard> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;
    private readonly CodeSetService _codes;

    public EZKartaController(IJsonRepository<PatientCard> repo, IdGenerator ids, SystemClock clock, CodeSetService codes)
    { _repo = repo; _ids = ids; _clock = clock; _codes = codes; }

    [HttpPost]
    public async Task<ActionResult<IdResponse>> Create([FromBody] PatientCard input, CancellationToken ct)
    {
        // Shallow code validation for sample
              foreach (var code in input.ActiveProblems ?? Array.Empty<string>())
        {
            if (!_codes.TryResolveIcd10(code, out _) && !_codes.TryResolveSnomed(code, out _))
            {
                               var errors = new Dictionary<string, string[]>
                {
                    ["activeProblems"] = new[] { $"Unknown code {code}" }
                };
                return UnprocessableEntity(new ValidationProblemDetails(errors)
                {
                    Type = "https://http.dev/errors/validation",
                    Title = "Validation failed"
                });
            }
        }
        var id = _ids.NewId();
        var entity = input with { Id = id, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, entity, ct);
        return CreatedAtAction(nameof(Get), new { id }, new IdResponse(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientCard>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<PatientCard>> Get(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } item ? Ok(item) : NotFound();
}
