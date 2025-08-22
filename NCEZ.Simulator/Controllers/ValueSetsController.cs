using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/valuesets")]
[Tags("Číselníky")]
public sealed class ValueSetsController : ControllerBase
{
    private readonly CodeSetService _svc;
    public ValueSetsController(CodeSetService svc) { _svc = svc; }

    [HttpGet]
    public ActionResult<IEnumerable<string>> List() => Ok(_svc.ValueSets.Keys.OrderBy(x => x));

    [HttpGet("{name}/expand")]
    public ActionResult<IEnumerable<CodeEntry>> Expand(string name, [FromQuery] string? filter = null, [FromQuery] int take = 200)
    {
        if (!_svc.ValueSets.TryGetValue(name, out var list)) return NotFound();
        IEnumerable<CodeEntry> src = list;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.Trim();
            src = src.Where(e => e.Code.Contains(f, StringComparison.OrdinalIgnoreCase) || e.Display.Contains(f, StringComparison.OrdinalIgnoreCase));
        }
        return Ok(src.Take(take));
    }

    // $expand s compose (minimální)
    public sealed record Include(string? System, string[]? Codes, string? Filter, string? Version);
    public sealed record Compose(Include[]? Include, Include[]? Exclude);

    [HttpPost("$expand")]
    public ActionResult<IEnumerable<CodeEntry>> ExpandCompose([FromBody] Compose body, [FromQuery] int take = 500)
    {
        var result = new List<CodeEntry>();

        if (body.Include is not null)
        {
            foreach (var inc in body.Include)
            {
                if (inc.Codes is { Length: > 0 } && !string.IsNullOrWhiteSpace(inc.System))
                {
                    result.AddRange(_svc.BatchGet(inc.System!, inc.Codes!, inc.Version));
                }
                else if (!string.IsNullOrWhiteSpace(inc.System))
                {
                    result.AddRange(_svc.Search(inc.System!, inc.Filter, 0, take, inc.Version));
                }
            }
        }

        if (body.Exclude is not null)
        {
            foreach (var exc in body.Exclude)
            {
                if (exc.Codes is { Length: > 0 } && !string.IsNullOrWhiteSpace(exc.System))
                {
                    var remove = _svc.BatchGet(exc.System!, exc.Codes!, exc.Version);
                    result.RemoveAll(e => remove.Any(r => r.System.Equals(e.System, StringComparison.OrdinalIgnoreCase) && r.Code.Equals(e.Code, StringComparison.OrdinalIgnoreCase)));
                }
                else if (!string.IsNullOrWhiteSpace(exc.System) && !string.IsNullOrWhiteSpace(exc.Filter))
                {
                    var remove = _svc.Search(exc.System!, exc.Filter, 0, int.MaxValue, exc.Version);
                    result.RemoveAll(e => remove.Any(r => r.System.Equals(e.System, StringComparison.OrdinalIgnoreCase) && r.Code.Equals(e.Code, StringComparison.OrdinalIgnoreCase)));
                }
            }
        }

        // dedupe
        var dedup = result
            .GroupBy(e => (e.System.ToLowerInvariant(), e.Code.ToLowerInvariant()))
            .Select(g => g.First())
            .Take(take)
            .ToList();

        return Ok(dedup);
    }
}