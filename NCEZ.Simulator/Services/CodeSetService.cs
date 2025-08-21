
namespace NCEZ.Simulator.Services;

public sealed class CodeSetService
{
    private readonly Dictionary<string, (string display, string system)> _icd10 = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string display, string system)> _snomed = new(StringComparer.OrdinalIgnoreCase);

    public CodeSetService()
    {
        // Minimal subsets. Extend by editing JSON files in Data/CodeSets.
        // ICD-10 (CZ subset examples)
        _icd10["A09"] = ("Průjmy a gastroenteritida pravděpodobně infekčního původu", "ICD-10-CZ");
        _icd10["I10"] = ("Esenciální (primární) hypertenze", "ICD-10-CZ");
        _icd10["E11"] = ("Diabetes mellitus 2. typu", "ICD-10-CZ");
        _icd10["J06.9"] = ("Akutní infekce horních dýchacích cest NS", "ICD-10-CZ");

        // SNOMED CT (illustrative subset)
        _snomed["44054006"] = ("Diabetes mellitus type 2 (disorder)", "http://snomed.info/sct");
        _snomed["38341003"] = ("Hypertensive disorder, systemic arterial (disorder)", "http://snomed.info/sct");
        _snomed["386661006"] = ("Fever (finding)", "http://snomed.info/sct");
        _snomed["27113001"] = ("Body temperature (observable entity)", "http://snomed.info/sct");
        _snomed["721981007"] = ("Laboratory test result (observable entity)", "http://snomed.info/sct");
    }

    public bool TryResolveIcd10(string code, out (string display, string system) result) => _icd10.TryGetValue(code, out result);
    public bool TryResolveSnomed(string code, out (string display, string system) result) => _snomed.TryGetValue(code, out result);
}
