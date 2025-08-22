using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace NCEZ.Simulator.Controllers;

[ApiController]
[Route("api/v1/_diagnostics")]
[Tags("Diagnostics")]
[Produces("application/json")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly IApiDescriptionGroupCollectionProvider _provider;
    public DiagnosticsController(IApiDescriptionGroupCollectionProvider provider) { _provider = provider; }

    public sealed record ApiRef(string Verb, string Path, string? Source = null);
    // ... původní kód zkrácen (zachován v původním souboru)

    private static string Norm(string? p)
    {
        var s = "/" + (p ?? "").Trim().Trim('/');
        while (s.Contains("//")) s = s.Replace("//", "/");
        return s;
    }
    private static string StripApiVersion(string p)
        => System.Text.RegularExpressions.Regex.Replace(p, "^/api/v\\d+", "");

    private IEnumerable<ApiRef> ProjectEndpoints()
    {
        foreach (var g in _provider.ApiDescriptionGroups.Items)
        foreach (var d in g.Items)
        {
            var verb = d.HttpMethod ?? "";
            var path = Norm(d.RelativePath);
            yield return new ApiRef(verb, StripApiVersion(path), "project");
        }
    }

    /// <summary>
    /// Simulátor FHIR zprávy ze zobrazovacího vyšetření.
    /// Vrací Bundle typu "message" s Patient, ServiceRequest, ImagingStudy, DiagnosticReport a DocumentReference (PDF v attachmentu).
    /// </summary>
    [HttpGet("fhir/imaging-report/example")]
    [Produces("application/fhir+json")]
    public IActionResult ImagingReportExample()
    {
        var now = DateTimeOffset.UtcNow;

        // Minimalní platný PDF jako base64 (embedded)
        var pdfData = Convert.FromBase64String(MINIMAL_PDF_BASE64);
        var pdfHash = Convert.ToBase64String(SHA1.HashData(pdfData));

        object entry(string fullUrl, object resource) => new { fullUrl, resource };

        var patientId = "urn:uuid:patient-1";
        var orgId = "urn:uuid:org-1";
        var pracId = "urn:uuid:pract-1";
        var srId = "urn:uuid:sr-1";
        var studyId = "urn:uuid:study-1";
        var drId = "urn:uuid:dr-1";
        var docRefId = "urn:uuid:docref-1";
        var obsId = "urn:uuid:obs-1";

        var bundle = new
        {
            resourceType = "Bundle",
            type = "message",
            timestamp = now.ToString("o"),
            entry = new object[]
            {
                entry(patientId, new {
                    resourceType = "Patient",
                    id = "patient-1",
                    identifier = new [] { new { system = "urn:oid:1.2.203.24341.1.2.1", value = "1234567890" } },
                    name = new [] { new { family = "Novák", given = new [] { "Jan" } } },
                    gender = "male",
                    birthDate = "1980-01-01"
                }),
                entry(orgId, new {
                    resourceType = "Organization",
                    id = "org-1",
                    name = "Nemocnice A"
                }),
                entry(pracId, new {
                    resourceType = "Practitioner",
                    id = "pract-1",
                    name = new [] { new { text = "MUDr. Lékař" } }
                }),
                entry(srId, new {
                    resourceType = "ServiceRequest",
                    id = "sr-1",
                    status = "completed",
                    intent = "order",
                    code = new { text = "CT břicha" },
                    subject = new { reference = patientId },
                    requester = new { reference = pracId },
                    reasonCode = new [] { new { text = "Bolesti břicha" } }
                }),
                entry(studyId, new {
                    resourceType = "ImagingStudy",
                    id = "study-1",
                    status = "available",
                    subject = new { reference = patientId },
                    basedOn = new [] { new { reference = srId } },
                    modality = new [] { new { system = "http://dicom.nema.org/resources/ontology/DCM", code = "CT", display = "Computed Tomography" } },
                    numberOfSeries = 1,
                    numberOfInstances = 1,
                    series = new [] { new {
                        uid = "1.2.276.0.7230010.3.1.4.8323329.1234.1592316580.1",
                        number = 1,
                        modality = new { system = "http://dicom.nema.org/resources/ontology/DCM", code = "CT" },
                        bodySite = new { text = "Abdomen" },
                        instance = new [] { new {
                            uid = "1.2.276.0.7230010.3.1.4.8323329.1234.1592316580.1.1",
                            sopClass = new { system = "urn:ietf:rfc:3986", code = "1.2.840.10008.5.1.4.1.1.2" } // CT Image Storage
                        } }
                    } }
                }),
                entry(docRefId, new {
                    resourceType = "DocumentReference",
                    id = "docref-1",
                    status = "current",
                    type = new { coding = new [] { new { system = "http://loinc.org", code = "LP29684-5", display = "Imaging report" } }, text = "Zpráva ze zobrazovacích metod" },
                    subject = new { reference = patientId },
                    date = now.ToString("o"),
                    content = new [] { new { attachment = new {
                        contentType = "application/pdf",
                        title = "Zpráva z obrazového vyšetření",
                        data = Convert.ToBase64String(pdfData),
                        hash = pdfHash
                    } } }
                }),
                entry(drId, new {
                    resourceType = "DiagnosticReport",
                    id = "dr-1",
                    status = "final",
                    code = new { coding = new [] { new { system = "http://loinc.org", code = "LP29684-5", display = "Imaging report" } }, text = "Zpráva ze zobrazovacích metod" },
                    subject = new { reference = patientId },
                    effectiveDateTime = now.ToString("o"),
                    issued = now.ToString("o"),
                    performer = new [] { new { reference = orgId } },
                    resultsInterpreter = new [] { new { reference = pracId } },
                    imagingStudy = new [] { new { reference = studyId } },
                    presentedForm = new [] { new { contentType = "application/pdf", title = "Zpráva z obrazového vyšetření", data = Convert.ToBase64String(pdfData), hash = pdfHash } },
                    conclusion = "Bez akutní patologie.",
                    conclusionCode = new [] { new { text = "Negativní nález" } }
                }),
                entry(obsId, new {
                    resourceType = "Observation",
                    id = "obs-1",
                    status = "final",
                    code = new { text = "Nález" },
                    subject = new { reference = patientId },
                    effectiveDateTime = now.ToString("o"),
                    valueString = "Popis vyšetření a nález"
                })
            }
        };

        return Ok(bundle);
    }

    private const string MINIMAL_PDF_BASE64 = """
JVBERi0xLjQKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUg
L1BhZ2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudCAyIDAg
UiAvTWVkaWFCb3ggWzAgMCA1OTUgODQyXSAvQ29udGVudHMgNCAwIFIgL1Jlc291cmNlcyA8PCAvRm9udCA8PCAvRjEgNSAwIFIg
Pj4gPj4gPj4KZW5kb2JqCjQgMCBvYmoKPDwgL0xlbmd0aCA1NSA+PgpzdHJlYW0KQlQgL0YxIDEyIFRmIDcyIDcyMCBUZCAoSW1h
Z2luZyByZXBvcnQgZXhhbXBsZSkgVGoKRUQKZW5kc3RyZWFtCmVuZG9iago1IDAgb2JqCjw8IC9UeXBlIC9Gb250IC9TdWJ0eXBl
IC9UeXBlMSAvQmFzZUZvbnQgL0hlbHZldGljYSA+PgolAGRvIG5pYzp4cmVmCjAgNgowMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAx
MCAwMDAwMCBuIAowMDAwMDA4NSAwMDAwMCBuIAowMDAwMDE4NCAwMDAwMCBuIAowMDAwMDI0OSAwMDAwMCBuIAowMDAwMDI4OCAw
MDAwMCBuIAp0cmFpbGVyCjw8IC9Sb290IDEgMCBSIC9TaXplIDYgPj4Kc3RhcnR4cmVmCjI5NAolJUVPRg==
""";
}