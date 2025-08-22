using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using NCEZ.Simulator.Models;

namespace NCEZ.Simulator.Services;

public sealed class CodeSetService
{
    private readonly string _folder;
    private readonly string _mapsFolder;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    // system -> (version -> (code -> entry))
    private readonly Dictionary<string, SortedDictionary<string, Dictionary<string, CodeEntry>>> _versions =
        new(StringComparer.OrdinalIgnoreCase);

    // system -> default (vybraná verze)
    private readonly Dictionary<string, Dictionary<string, CodeEntry>> _systems =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, CodeSystemMeta> _meta =
        new(StringComparer.OrdinalIgnoreCase);

    private List<ConceptMapEntry> _conceptMaps = new();

    private readonly object _gate = new();

    public CodeSetService(IHostEnvironment env)
    {
        _folder = Path.Combine(env.ContentRootPath, "Data", "CodeSets");
        _mapsFolder = Path.Combine(_folder, "ConceptMaps");
        Directory.CreateDirectory(_folder);
        Directory.CreateDirectory(_mapsFolder);
        LoadAll();
        LoadConceptMaps();
        Watch();
    }

    #region Load

    private sealed class FileEntry
    {
        public string? System { get; set; }
        public string Code { get; set; } = default!;
        public string Display { get; set; } = default!;
        public string? Version { get; set; }
        public string? Lang { get; set; }
        public string[]? Synonyms { get; set; }
        public string[]? Parents { get; set; }
        public bool? Active { get; set; }
    }

    private static (string sys, string? ver) ParseName(string fileBase)
    {
        // icd10@2024 -> (icd10, 2024), jinak (name, null)
        var i = fileBase.IndexOf('@');
        if (i > 0) return (fileBase[..i], fileBase[(i + 1)..]);
        return (fileBase, null);
    }

    private static string TitleFor(string sys) => sys.ToLowerInvariant() switch
    {
        "icd10" => "ICD-10-CZ",
        "snomed" => "SNOMED CT",
        "loinc" => "LOINC",
        "atc" => "ATC",
        "icpc2" => "ICPC-2",
        "ucum" => "UCUM",
        _ => sys.ToUpperInvariant()
    };

    public void LoadAll()
    {
        lock (_gate)
        {
            _versions.Clear();
            _systems.Clear();
            _meta.Clear();

            foreach (var f in Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    if (string.Equals(baseName, "icd10-sample", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(baseName, "snomed-sample", StringComparison.OrdinalIgnoreCase)) continue;

                    var (raw, ver) = ParseName(baseName);
                    var sys = NormalizeSystemKey(raw);

                    var list = ReadEntries(f, sys, ver);
                    if (!_versions.TryGetValue(sys, out var verDict))
                    {
                        verDict = new(StringComparer.OrdinalIgnoreCase);
                        _versions[sys] = verDict;
                    }
                    var versionKey = ver ?? "default";
                    var dict = new Dictionary<string, CodeEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var e in list)
                    {
                        if (e is null) continue;
                        if (string.IsNullOrWhiteSpace(e.Code)) continue; // skip nevalidní záznam
                        dict[e.Code] = e; // poslední vyhrává
                    }
                    verDict[versionKey] = dict;
                }
                catch
                {
                    // nevalidní soubor – ignoruj, ať služba běží
                }
            }

            // built-ins if missing
            EnsureBuiltIns();

            // pick default version per system
            foreach (var kv in _versions)
            {
                var sys = kv.Key;
                var pick = kv.Value.Keys.First();
                // last lexicographical as default
                foreach (var k in kv.Value.Keys) pick = string.CompareOrdinal(k, pick) > 0 ? k : pick;
                _systems[sys] = kv.Value[pick];
                _meta[sys] = new CodeSystemMeta(sys, TitleFor(sys), pick == "default" ? null : pick, _systems[sys].Count, DateTimeOffset.UtcNow);
            }
        }
    }

    private static IEnumerable<CodeEntry> ReadEntries(string path, string sys, string? ver)
    {
        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);

        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var code = prop.Name;
                if (string.IsNullOrWhiteSpace(code)) continue;
                var disp = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : null;
                yield return new CodeEntry { System = sys, Code = code, Display = disp ?? code, Version = ver };
            }
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var arr = JsonSerializer.Deserialize<List<FileEntry>>(File.ReadAllText(path)) ?? new();
            foreach (var e in arr)
            {
                if (e is null) continue;
                if (string.IsNullOrWhiteSpace(e.Code)) continue;
                var disp = string.IsNullOrWhiteSpace(e.Display) ? e.Code : e.Display;
                yield return new CodeEntry
                {
                    System = string.IsNullOrWhiteSpace(e.System) ? sys : e.System!,
                    Code = e.Code,
                    Display = disp!,
                    Version = string.IsNullOrWhiteSpace(e.Version) ? ver : e.Version,
                    Lang = e.Lang,
                    Synonyms = e.Synonyms,
                    Parents = e.Parents,
                    Active = e.Active
                };
            }
        }
    }

    private void EnsureBuiltIns()
    {
        void AddIfMissing(string sys, IEnumerable<CodeEntry> items)
        {
            if (!_versions.ContainsKey(sys))
                _versions[sys] = new(StringComparer.OrdinalIgnoreCase);
            if (!_versions[sys].ContainsKey("default"))
                _versions[sys]["default"] = new(StringComparer.OrdinalIgnoreCase);

            foreach (var e in items)
                _versions[sys]["default"][e.Code] = e with { System = sys, Version = e.Version ?? "default" };
        }

        if (!_versions.ContainsKey("icd10"))
        {
            AddIfMissing("icd10", new[]
            {
                new CodeEntry{ System="icd10", Code="A09",  Display="Průjmy a gastroenteritida pravděpodobně infekčního původu"},
                new CodeEntry{ System="icd10", Code="I10",  Display="Esenciální (primární) hypertenze"},
                new CodeEntry{ System="icd10", Code="E11",  Display="Diabetes mellitus 2. typu"},
                new CodeEntry{ System="icd10", Code="J06.9",Display="Akutní infekce horních dýchacích cest NS"}
            });
        }
        if (!_versions.ContainsKey("snomed"))
        {
            AddIfMissing("snomed", new[]
            {
                new CodeEntry{ System="snomed", Code="44054006",  Display="Diabetes mellitus type 2 (disorder)"},
                new CodeEntry{ System="snomed", Code="38341003",  Display="Hypertensive disorder, systemic arterial (disorder)"},
                new CodeEntry{ System="snomed", Code="386661006", Display="Fever (finding)"},
                new CodeEntry{ System="snomed", Code="27113001",  Display="Body temperature (observable entity)"}
            });
        }
        if (!_versions.ContainsKey("ucum"))
        {
            AddIfMissing("ucum", new[]
            {
                new CodeEntry{ System="ucum", Code="kg", Display="kilogram" },
                new CodeEntry{ System="ucum", Code="g",  Display="gram" },
                new CodeEntry{ System="ucum", Code="mg", Display="milligram" },
                new CodeEntry{ System="ucum", Code="L",  Display="liter" },
                new CodeEntry{ System="ucum", Code="mL", Display="milliliter" }
            });
        }
    }

    private static string NormalizeSystemKey(string fileBase)
    {
        var n = fileBase.ToLowerInvariant();
        if (n.Contains("icd10")) return "icd10";
        if (n.Contains("snomed")) return "snomed";
        if (n.Contains("loinc")) return "loinc";
        if (n.Contains("atc")) return "atc";
        if (n.Contains("icpc")) return "icpc2";
        if (n.Contains("ucum")) return "ucum";
        return n;
    }

     private void LoadConceptMaps()
    {
        var tmp = new List<ConceptMapEntry>();
       foreach (var f in Directory.EnumerateFiles(_mapsFolder, "*.json"))
        {
            var arr = JsonSerializer.Deserialize<List<ConceptMapEntry>>(File.ReadAllText(f), _json);
            if (arr is { Count: > 0 }) tmp.AddRange(arr);
        }
        lock (_gate) { _conceptMaps = tmp; }
    }

    private void Watch()
    {
        var w1 = new FileSystemWatcher(_folder, "*.json") { IncludeSubdirectories = false, EnableRaisingEvents = true };
        w1.Changed += (_, __) => SafeReload();
        w1.Created += (_, __) => SafeReload();
        w1.Deleted += (_, __) => SafeReload();
        var w2 = new FileSystemWatcher(_mapsFolder, "*.json") { IncludeSubdirectories = false, EnableRaisingEvents = true };
        w2.Changed += (_, __) => SafeReloadMaps();
        w2.Created += (_, __) => SafeReloadMaps();
        w2.Deleted += (_, __) => SafeReloadMaps();
    }
    private void SafeReload() { try { LoadAll(); } catch { /* swallow */ } }
    private void SafeReloadMaps() { try { LoadConceptMaps(); } catch { /* swallow */ } }

    #endregion

    public IReadOnlyList<CodeSystemMeta> Systems()
    {
        lock (_gate) return _meta.Values.OrderBy(m => m.System).ToList();
    }

    public IReadOnlyList<string> Versions(string system)
    {
        lock (_gate)
        {
            return _versions.TryGetValue(system, out var vers)
                ? vers.Keys.ToList()
                : Array.Empty<string>();
        }
    }

    private Dictionary<string, CodeEntry>? Dict(string system, string? version)
    {
        if (!_versions.TryGetValue(system, out var vers)) return null;
        if (string.IsNullOrWhiteSpace(version))
        {
            return _systems.TryGetValue(system, out var latest) ? latest : vers.Values.LastOrDefault();
        }
        return vers.TryGetValue(version!, out var dict) ? dict : null;
    }

    public bool TryGet(string system, string code, out CodeEntry entry, string? version = null)
    {
        lock (_gate)
        {
            entry = null!;
            var dict = Dict(system, version);
            if (dict is null) return false;
            return dict.TryGetValue(code, out entry!);
        }
    }

    public IReadOnlyList<CodeEntry> BatchGet(string system, IEnumerable<string> codes, string? version = null)
    {
        var result = new List<CodeEntry>();
        lock (_gate)
        {
            var dict = Dict(system, version);
            if (dict is null) return result;
            foreach (var c in codes.Distinct(StringComparer.OrdinalIgnoreCase))
                if (dict.TryGetValue(c, out var e)) result.Add(e);
        }
        return result;
    }

    public IReadOnlyList<CodeEntry> Search(string system, string? q, int skip, int take, string? version = null, string? regex = null, bool startsWith = false, string? sort = null)
    {
        lock (_gate)
        {
            IEnumerable<CodeEntry> src;
            if (string.Equals(system, "all", StringComparison.OrdinalIgnoreCase))
                src = _systems.Values.SelectMany(d => d.Values);
            else
            {
                var dict = Dict(system, version);
                if (dict is null) return Array.Empty<CodeEntry>();
                src = dict.Values;
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var tokens = Tokenize(q);
                src = src.Where(e => Matches(e, tokens, startsWith));
            }

            if (!string.IsNullOrWhiteSpace(regex))
            {
                var re = new Regex(regex!, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                src = src.Where(e => re.IsMatch(e.Code) || re.IsMatch(e.Display));
            }

            src = ScoreOrder(src, q);

            src = sort?.ToLowerInvariant() switch
            {
                "code" => src.OrderBy(e => e.Code, StringComparer.OrdinalIgnoreCase),
                "display" => src.OrderBy(e => e.Display, StringComparer.OrdinalIgnoreCase),
                _ => src
            };

            return src.Skip(skip).Take(take <= 0 ? 50 : take).ToList();
        }
    }

    public IReadOnlyList<CodeEntry> Suggest(string system, string q, int limit = 20, string? version = null)
        => Search(system, q, 0, limit, version, regex: null, startsWith: true);

    public IReadOnlyList<ValidateCodesResult> Validate(string system, IEnumerable<string> codes, string? version = null)
    {
        var list = new List<ValidateCodesResult>();
        lock (_gate)
        {
            var dict = Dict(system, version);
            foreach (var c in codes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (dict is not null && dict.TryGetValue(c, out var e))
                    list.Add(new ValidateCodesResult(c, true, e.Display, system));
                else list.Add(new ValidateCodesResult(c, false, null, system));
            }
        }
        return list;
    }

    public OperationOutcome ValidateCoding(string system, string code, string? display, string? version = null)
    {
        if (!TryGet(system, code, out var entry, version))
            return new OperationOutcome(new[] { new OperationOutcomeIssue("error", "code-invalid", $"Code '{system}|{code}' not found") });

        if (!string.IsNullOrWhiteSpace(display) && !string.Equals(display, entry.Display, StringComparison.OrdinalIgnoreCase))
            return new OperationOutcome(new[]
            {
                new OperationOutcomeIssue("warning", "display-mismatch", $"Display differs. Expected '{entry.Display}'.")
            });

        return new OperationOutcome(new[] { new OperationOutcomeIssue("information", "ok", "Valid coding") });
    }

    public IReadOnlyList<ConceptMapEntry> Map(string fromSystem, string toSystem, string code)
    {
        List<ConceptMapEntry> snapshot;
        lock (_gate) { snapshot = _conceptMaps; }
        return snapshot.Where(m =>
               string.Equals(m.SourceSystem, fromSystem, StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.TargetSystem, toSystem, StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.SourceCode, code, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyDictionary<string, List<CodeEntry>> ValueSets => _valueSets;

    private readonly Dictionary<string, List<CodeEntry>> _valueSets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hl7-administrative-gender"] = new List<CodeEntry>
        {
            new CodeEntry{ System="http://hl7.org/fhir/administrative-gender", Code="male",    Display="Male" },
            new CodeEntry{ System="http://hl7.org/fhir/administrative-gender", Code="female",  Display="Female" },
            new CodeEntry{ System="http://hl7.org/fhir/administrative-gender", Code="other",   Display="Other" },
            new CodeEntry{ System="http://hl7.org/fhir/administrative-gender", Code="unknown", Display="Unknown" }
        }
    };

    #region Helpers: search

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
        return noDiacritics.ToLowerInvariant();
    }

    private static string[] Tokenize(string q)
        => Normalize(q).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool Matches(CodeEntry e, string[] tokens, bool startsWith)
    {
        var codeN = Normalize(e.Code);
        var dispN = Normalize(e.Display);
        var synN = e.Synonyms?.Select(Normalize).ToArray() ?? Array.Empty<string>();

        foreach (var t in tokens)
        {
            var ok =
                codeN.Contains(t) ||
                dispN.Contains(t) ||
                synN.Any(s => s.Contains(t));

            if (startsWith)
                ok = codeN.StartsWith(t) || dispN.StartsWith(t) || synN.Any(s => s.StartsWith(t));

            if (!ok) return false;
        }
        return true;
    }

    private static IEnumerable<CodeEntry> ScoreOrder(IEnumerable<CodeEntry> src, string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return src;
        var term = Normalize(q);
        return src.Select(e =>
        {
            var codeN = Normalize(e.Code);
            var dispN = Normalize(e.Display);
            var synN = e.Synonyms?.Select(Normalize) ?? Enumerable.Empty<string>();

            int score = 0;
            if (codeN.Equals(term)) score += 100;
            if (codeN.StartsWith(term)) score += 80;
            if (dispN.StartsWith(term)) score += 70;
            if (dispN.Contains(term)) score += 60;
            if (synN.Any(s => s.StartsWith(term))) score += 50;
            if (synN.Any(s => s.Contains(term))) score += 40;

            return (e, score);
        })
        .OrderByDescending(t => t.score)
        .ThenBy(t => t.e.Code, StringComparer.OrdinalIgnoreCase)
        .Select(t => t.e);
    }

    #endregion
}