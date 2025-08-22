using System.Text.Json;

namespace NCEZ.Simulator.Services
{
    public sealed class HdrValidator
    {
        public sealed record Error(string Code, string Message, string? Path = null);

        public IReadOnlyList<Error> Validate(JsonDocument bundle)
        {
            var errors = new List<Error>();
            if (bundle is null)
            {
                errors.Add(new Error("bundle.null", "Body is null"));
                return errors;
            }

            var root = bundle.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new Error("bundle.type", "Body must be a JSON object", "$"));
                return errors;
            }

            if (!root.TryGetProperty("resourceType", out var rt) || rt.ValueKind != JsonValueKind.String)
                errors.Add(new Error("bundle.resourceType", "Missing resourceType='Bundle'", "$.resourceType"));
            else if (!string.Equals(rt.GetString(), "Bundle", StringComparison.OrdinalIgnoreCase))
                errors.Add(new Error("bundle.resourceType", "resourceType must be 'Bundle'", "$.resourceType"));

            if (!root.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
                errors.Add(new Error("bundle.type", "Missing Bundle.type='document'", "$.type"));
            else if (!string.Equals(typeEl.GetString(), "document", StringComparison.OrdinalIgnoreCase))
                errors.Add(new Error("bundle.type", "Bundle.type must be 'document'", "$.type"));

            if (!root.TryGetProperty("entry", out var entry) || entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() == 0)
                errors.Add(new Error("bundle.entry", "Bundle.entry must be a non-empty array", "$.entry"));
            else
            {
                var first = entry[0];
                if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("resource", out var res) && res.ValueKind == JsonValueKind.Object)
                {
                    if (!res.TryGetProperty("resourceType", out var rrt) || rrt.ValueKind != JsonValueKind.String || rrt.GetString() != "Composition")
                        errors.Add(new Error("composition.missing", "First entry.resource must be Composition", "$.entry[0].resource"));
                }
                else
                {
                    errors.Add(new Error("entry.resource", "entry[0].resource must be object", "$.entry[0].resource"));
                }
            }

            return errors;
        }
    }
}