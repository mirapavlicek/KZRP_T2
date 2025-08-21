
using Microsoft.AspNetCore.Mvc;
using NCEZ.Simulator.Models;
using NCEZ.Simulator.Services;
using System.Security.Cryptography;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/temp-storage")]
[Tags("Dočasné úložiště")]
public sealed class TemporaryStorageController : ControllerBase
{
    private readonly IJsonRepository<TemporaryDocument> _repo;
    private readonly IdGenerator _ids;
    private readonly SystemClock _clock;
    private readonly StorageOptions _opts;

    public TemporaryStorageController(IJsonRepository<TemporaryDocument> repo, IdGenerator ids, SystemClock clock, StorageOptions opts)
    {
        _repo = repo; _ids = ids; _clock = clock; _opts = opts;
    }

    [HttpPost("documents")]
    [RequestSizeLimit(100_000_000)] // 100 MB
    public async Task<ActionResult<IdResponse>> Upload([FromForm] IFormFile file, [FromForm] string? uploaderId, [FromForm] string[]? tags, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("Empty file.");
        var id = _ids.NewId();
        var folder = Path.Combine(_opts.DataRoot, "Binary", "TemporaryDocument");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, id);
        await using var fs = System.IO.File.Create(path);
        await file.CopyToAsync(fs, ct);
        await fs.FlushAsync(ct);
        fs.Close();

        // Compute hash
        using var sha = SHA256.Create();
        await using var fs2 = System.IO.File.OpenRead(path);
        var hash = Convert.ToHexString(await sha.ComputeHashAsync(fs2, ct));

        var meta = new TemporaryDocument
        {
            Id = id,
            Name = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Sha256 = hash,
            StoragePath = path,
            UploaderId = uploaderId,
            Tags = tags ?? Array.Empty<string>(),
            CreatedAt = _clock.Now
        };
        await _repo.UpsertAsync(id, meta, ct);
        return CreatedAtAction(nameof(GetMetadata), new { id }, new IdResponse(id));
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IEnumerable<TemporaryDocument>>> List(int skip = 0, int take = 100, CancellationToken ct = default)
        => Ok(await _repo.ListAsync(skip, take, ct));

    [HttpGet("documents/{id}")]
    public async Task<ActionResult<TemporaryDocument>> GetMetadata(string id, CancellationToken ct)
        => (await _repo.GetAsync(id, ct)) is { } meta ? Ok(meta) : NotFound();

    [HttpGet("documents/{id}/content")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var meta = await _repo.GetAsync(id, ct);
        if (meta is null || !System.IO.File.Exists(meta.StoragePath)) return NotFound();
        var stream = System.IO.File.OpenRead(meta.StoragePath);
        return File(stream, meta.ContentType, meta.Name);
    }

    [HttpDelete("documents/{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var meta = await _repo.GetAsync(id, ct);
        if (meta is null) return NotFound();
        if (System.IO.File.Exists(meta.StoragePath)) System.IO.File.Delete(meta.StoragePath);
        await _repo.DeleteAsync(id, ct);
        return NoContent();
    }
}
