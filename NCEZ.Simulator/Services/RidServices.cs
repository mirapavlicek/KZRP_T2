using System.Security.Cryptography;
using System.Text;
using NCEZ.Simulator.Models;

namespace NCEZ.Simulator.Services;

public sealed class RidService
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    private readonly IJsonRepository<RidAllocation> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;

    public RidService(IJsonRepository<RidAllocation> repo, IdGenerator ids, SystemClock clock)
    { _repo = repo; _ids = ids; _clock = clock; }

    private async Task<bool> ExistsAsync(string value, CancellationToken ct)
        => (await _repo.SearchAsync(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase), 0, 1, ct)).Count > 0;

    public static bool IsValidRid(string rid)
    {
        if (string.IsNullOrWhiteSpace(rid) || rid.Length != 10) return false;
        if (rid[0] == '0' || !rid.All(char.IsDigit)) return false;
        var num = long.Parse(rid);
        if (num % 13 != 0) return false;
        if (num % 11 == 0) return false;
        return true;
    }

    public static bool IsValidDrid(string drid)
    {
        if (string.IsNullOrWhiteSpace(drid) || drid.Length != 10) return false;
        if (drid[0] != 'D') return false;
        for (int i = 1; i < 10; i++) if (!char.IsDigit(drid[i])) return false;
        return true;
    }

    private static string RandomDigits(int n)
    {
        var bytes = new byte[n];
        Rng.GetBytes(bytes);
        var sb = new StringBuilder(n);
        foreach (var b in bytes) sb.Append((b % 10).ToString());
        return sb.ToString();
    }

    private static char RandomNonZeroDigit()
    {
        var b = new byte[1];
        do { Rng.GetBytes(b); } while ((b[0] % 10) == 0);
        return (char)('0' + (b[0] % 10));
    }

    public async Task<RidAllocation> AllocateDridAsync(CancellationToken ct)
    {
        string value;
        int guard = 0;
        do
        {
            value = "D" + RandomDigits(9);
            guard++;
        } while ((!IsValidDrid(value) || await ExistsAsync(value, ct)) && guard < 2000);

        var id = _ids.NewId();
        var alloc = new RidAllocation { Id = id, Type = "DRID", Value = value, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, alloc, ct);
        return alloc;
    }

    public async Task<IReadOnlyList<RidAllocation>> AllocateDridBatchAsync(int count, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 1000);
        var list = new List<RidAllocation>(count);
        for (int i = 0; i < count; i++) list.Add(await AllocateDridAsync(ct));
        return list;
    }

    public async Task<RidAllocation> AllocateRidAsync(CancellationToken ct)
    {
        string value;
        int guard = 0;
        do
        {
            value = RandomNonZeroDigit() + RandomDigits(9);
            guard++;
        } while ((!IsValidRid(value) || await ExistsAsync(value, ct)) && guard < 10000);

        var id = _ids.NewId();
        var alloc = new RidAllocation { Id = id, Type = "RID", Value = value, CreatedAt = _clock.Now };
        await _repo.UpsertAsync(id, alloc, ct);
        return alloc;
    }

    public async Task<IReadOnlyList<RidAllocation>> AllocateRidBatchAsync(int count, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 1000);
        var list = new List<RidAllocation>(count);
        for (int i = 0; i < count; i++) list.Add(await AllocateRidAsync(ct));
        return list;
    }
}