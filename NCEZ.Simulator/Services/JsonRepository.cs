
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCEZ.Simulator.Services;

public interface IJsonRepository<T> where T : class
{
    Task<T?> GetAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default);
    Task UpsertAsync(string id, T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> SearchAsync(Func<T, bool> predicate, int skip = 0, int take = 100, CancellationToken ct = default);
}

public sealed class JsonRepository<T> : IJsonRepository<T> where T : class
{
    private readonly string _folder;
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonRepository(StorageOptions options)
    {
        _folder = Path.Combine(options.DataRoot, typeof(T).Name);
        Directory.CreateDirectory(_folder);
    }

    private string PathFor(string id) => System.IO.Path.Combine(_folder, $"{id}.json");

    public async Task<T?> GetAsync(string id, CancellationToken ct = default)
    {
        var path = PathFor(id);
        if (!File.Exists(path)) return null;
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
    }

    public async Task<IReadOnlyList<T>> ListAsync(int skip = 0, int take = 100, CancellationToken ct = default)
    {
        var files = Directory.EnumerateFiles(_folder, "*.json").Skip(skip).Take(take);
        var list = new List<T>();
        foreach (var f in files)
        {
            using var stream = File.OpenRead(f);
            var item = await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
            if (item != null) list.Add(item);
        }
        return list;
    }

    public async Task UpsertAsync(string id, T entity, CancellationToken ct = default)
    {
        var path = PathFor(id);
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, entity, _json, ct);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var path = PathFor(id);
        if (!File.Exists(path)) return Task.FromResult(false);
        File.Delete(path);
        return Task.FromResult(true);
    }

    public async Task<IReadOnlyList<T>> SearchAsync(Func<T, bool> predicate, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        var files = Directory.EnumerateFiles(_folder, "*.json");
        var list = new List<T>();
        foreach (var f in files)
        {
            using var stream = File.OpenRead(f);
            var item = await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
            if (item != null && predicate(item)) list.Add(item);
        }
        return list.Skip(skip).Take(take).ToList();
    }
}
