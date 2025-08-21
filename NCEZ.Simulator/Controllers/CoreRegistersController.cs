
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/registers")]
[Tags("Kmenové registry")]
public sealed class CoreRegistersController : ControllerBase
{
    private readonly IJsonRepository<Patient> _patients;
    private readonly IJsonRepository<Practitioner> _practitioners;
    private readonly IJsonRepository<Provider> _providers;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public CoreRegistersController(
        IJsonRepository<Patient> patients,
        IJsonRepository<Practitioner> practitioners,
        IJsonRepository<Provider> providers,
        IdGenerator ids,
        SystemClock clock)
    {
        _patients = patients; _practitioners = practitioners; _providers = providers; _ids = ids; _clock = clock;
    }

    // Patients
    [HttpPost("patients")]
    public async Task<ActionResult<IdResponse>> CreatePatient([FromBody] Patient input, CancellationToken ct)
    {
        var id = _ids.NewId();
        await _patients.UpsertAsync(id, input with { Id = id, CreatedAt = _clock.Now }, ct);
        return CreatedAtAction(nameof(GetPatient), new { id }, new IdResponse(id));
    }

    [HttpGet("patients/{id}")]
    public async Task<ActionResult<Patient>> GetPatient(string id, CancellationToken ct)
        => (await _patients.GetAsync(id, ct)) is { } p ? Ok(p) : NotFound();

    [HttpGet("patients")]
    public async Task<ActionResult<IEnumerable<Patient>>> ListPatients(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _patients.ListAsync(skip, take, ct));

    // Practitioners
    [HttpPost("practitioners")]
    public async Task<ActionResult<IdResponse>> CreatePractitioner([FromBody] Practitioner input, CancellationToken ct)
    {
        var id = _ids.NewId();
        await _practitioners.UpsertAsync(id, input with { Id = id, CreatedAt = _clock.Now }, ct);
        return CreatedAtAction(nameof(GetPractitioner), new { id }, new IdResponse(id));
    }

    [HttpGet("practitioners/{id}")]
    public async Task<ActionResult<Practitioner>> GetPractitioner(string id, CancellationToken ct)
        => (await _practitioners.GetAsync(id, ct)) is { } p ? Ok(p) : NotFound();

    [HttpGet("practitioners")]
    public async Task<ActionResult<IEnumerable<Practitioner>>> ListPractitioners(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _practitioners.ListAsync(skip, take, ct));

    // Providers
    [HttpPost("providers")]
    public async Task<ActionResult<IdResponse>> CreateProvider([FromBody] Provider input, CancellationToken ct)
    {
        var id = _ids.NewId();
        await _providers.UpsertAsync(id, input with { Id = id, CreatedAt = _clock.Now }, ct);
        return CreatedAtAction(nameof(GetProvider), new { id }, new IdResponse(id));
    }

    [HttpGet("providers/{id}")]
    public async Task<ActionResult<Provider>> GetProvider(string id, CancellationToken ct)
        => (await _providers.GetAsync(id, ct)) is { } p ? Ok(p) : NotFound();

    [HttpGet("providers")]
    public async Task<ActionResult<IEnumerable<Provider>>> ListProviders(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _providers.ListAsync(skip, take, ct));
}
