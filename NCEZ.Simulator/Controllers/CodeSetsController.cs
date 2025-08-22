using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/codes")]
[Tags("Číselníky")]
[Produces("application/json")]
public sealed class CodeSetsController : ControllerBase
{
    private readonly CodeSetService _svc;
    public CodeSetsController(CodeSetService svc) { _svc = svc; }

    [HttpGet("systems")]
    public ActionResult<IEnumerable<CodeSystemMeta>> Systems() => Ok(_svc.Systems());

    [HttpGet("{system}/versions")]
    public ActionResult<IEnumerable<string>> Versions(string system) => Ok(_svc.Versions(system));

    [HttpPost("systems/reload")]
    public ActionResult Reload() { _svc.LoadAll(); return NoContent(); }

    [HttpGet("{system}")]
    public ActionResult<IEnumerable<CodeEntry>> Search(
        string system,
        [FromQuery] string? q = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? version = null,
        [FromQuery] string? regex = null,
        [FromQuery] string? sort = null)
        => Ok(_svc.Search(system, q, skip, take, version, regex, startsWith: false, sort));

    [HttpGet("{system}/suggest")]
    public ActionResult<IEnumerable<CodeEntry>> Suggest(string system, [FromQuery] string q, [FromQuery] int limit = 20, [FromQuery] string? version = null)
        => Ok(_svc.Suggest(system, q, limit, version));

    [HttpGet("{system}/{code}")]
    public ActionResult<CodeEntry> Get(string system, string code, [FromQuery] string? version = null)
        => _svc.TryGet(system, code, out var e, version) ? Ok(e) : NotFound();

    public sealed record BatchGetRequest(string[] Codes, string? Version);

    [HttpPost("{system}/get")]
    public ActionResult<IEnumerable<CodeEntry>> BatchGet(string system, [FromBody] BatchGetRequest body)
        => Ok(_svc.BatchGet(system, body.Codes ?? Array.Empty<string>(), body.Version));

    [HttpGet("{system}/$export")]
    public ActionResult<IEnumerable<CodeEntry>> Export(string system, [FromQuery] string? version = null)
        => Ok(_svc.Search(system, q: null, skip: 0, take: int.MaxValue, version: version));

    public sealed record ValidateCodingRequest(string System, string Code, string? Display, string? Version);

    [HttpPost("$validate-coding")]
    public ActionResult<OperationOutcome> ValidateCoding([FromBody] ValidateCodingRequest body)
        => Ok(_svc.ValidateCoding(body.System, body.Code, body.Display, body.Version));

    public sealed record ValidateRequest(string[] Codes, string? Version);

    [HttpPost("{system}/validate")]
    public ActionResult<IEnumerable<ValidateCodesResult>> Validate(string system, [FromBody] ValidateRequest body)
        => Ok(_svc.Validate(system, body.Codes ?? Array.Empty<string>(), body.Version));

    // Mapování (ConceptMap)
    [HttpGet("map")]
    public ActionResult<IEnumerable<ConceptMapEntry>> Map([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? code)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new ProblemDetails{
                Type="https://http.dev/errors/validation",
                Title="Missing query parameters",
                Detail="Use ?from=<system>&to=<system>&code=<code>"
            });
        }
        return Ok(_svc.Map(from!, to!, code!));
    }

    public sealed record MapBatchRequest(string From, string To, string[] Codes);

    [HttpPost("map/batch")]
    public ActionResult<Dictionary<string, List<ConceptMapEntry>>> MapBatch([FromBody] MapBatchRequest body)
    {
        var dict = new Dictionary<string, List<ConceptMapEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in body.Codes ?? Array.Empty<string>())
            dict[c] = _svc.Map(body.From, body.To, c).ToList();
        return Ok(dict);
    }
}