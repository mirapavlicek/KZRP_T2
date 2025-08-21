
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/test")]
[Tags("Testovací rámec")]
public sealed class TestFrameworkController : ControllerBase
{
    private readonly IJsonRepository<Patient> _patients;
    private readonly IJsonRepository<Practitioner> _practitioners;
    private readonly IJsonRepository<Provider> _providers;
    private readonly IJsonRepository<NotificationMessage> _notifications;
    private readonly IJsonRepository<Requisition> _orders;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public TestFrameworkController(
        IJsonRepository<Patient> patients,
        IJsonRepository<Practitioner> practitioners,
        IJsonRepository<Provider> providers,
        IJsonRepository<NotificationMessage> notifications,
        IJsonRepository<Requisition> orders,
        IdGenerator ids,
        SystemClock clock)
    {
        _patients = patients; _practitioners = practitioners; _providers = providers;
        _notifications = notifications; _orders = orders; _ids = ids; _clock = clock;
    }

    [HttpPost("seed")]
    public async Task<ActionResult<object>> Seed([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var rnd = new Random(1234);
        for (int i = 0; i < count; i++)
        {
            var pid = _ids.NewId();
            await _patients.UpsertAsync(pid, new Patient
            {
                Id = pid,
                Identifier = $"RC{rnd.Next(100000, 999999)}/{rnd.Next(100, 999)}",
                GivenName = $"Jana{i}",
                FamilyName = $"Nováková{i}",
                BirthDate = DateTime.Today.AddDays(-rnd.Next(6000, 30000))
            }, ct);

            var pracId = _ids.NewId();
            await _practitioners.UpsertAsync(pracId, new Practitioner
            {
                Id = pracId,
                Identifier = $"ICP{rnd.Next(10000, 99999)}",
                GivenName = "MUDr.",
                FamilyName = $"Lékař{i}",
                Role = "Praktický lékař"
            }, ct);

            var provId = _ids.NewId();
            await _providers.UpsertAsync(provId, new Provider
            {
                Id = provId,
                Identifier = $"ICO{rnd.Next(10000000, 99999999)}",
                Name = $"Poliklinika {i}"
            }, ct);

            var reqId = _ids.NewId();
            await _orders.UpsertAsync(reqId, new Requisition
            {
                Id = reqId,
                PatientId = pid,
                RequestingProviderId = provId,
                Type = (i % 2 == 0) ? "Lab" : "Imaging",
                Status = OrderStatus.entered,
                OrderedAt = _clock.Now
            }, ct);

            var nId = _ids.NewId();
            await _notifications.UpsertAsync(nId, new NotificationMessage
            {
                Id = nId,
                Type = "OrderCreated",
                Topic = "eŽádanky",
                Payload = new { orderId = reqId, patientId = pid },
                Status = "new",
                OccurredAt = _clock.Now,
                CreatedAt = _clock.Now
            }, ct);
        }
        return Ok(new { seeded = count });
    }

    [HttpGet("status")]
    public ActionResult<object> Status() => Ok(new
    {
        message = "Test endpoints ready. Use POST /api/v1/test/seed to generate data."
    });
}
