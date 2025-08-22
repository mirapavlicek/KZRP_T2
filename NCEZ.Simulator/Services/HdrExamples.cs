namespace NCEZ.Simulator.Services;

public static class HdrExamples
{
    public static string Minimal() => """
{
  "resourceType":"Bundle",
  "type":"document",
  "entry":[
    { "resource":{
        "resourceType":"Patient","id":"p1","name":[{"family":"Novák","given":["Petr"]}]
    }},
    { "resource":{
      "resourceType":"Practitioner","id":"pr1","name":[{"family":"Lékař","given":["Jan"]}]
    }},
    { "resource":{
      "resourceType":"Composition",
      "status":"final",
      "type":{"text":"CZ HDR"},
      "date":"2025-01-01T10:00:00Z",
      "title":"Hospital Discharge Report",
      "subject":{"reference":"Patient/p1"},
      "author":[{"reference":"Practitioner/pr1"}],
      "attester":[{"mode":"legal","time":"2025-01-01T10:05:00Z","party":{"reference":"Practitioner/pr1"}}],
      "section":[
        {"title":"A.2.2 Urgent Information"},
        {"title":"A.2.3 Hospitalization Data"},
        {"title":"A.2.7 Course of Hospitalization"},
        {"title":"A.2.8 Condition at Discharge"}
      ]
    }}
  ]
}
""";

    public static string Full() => """
{
  "resourceType":"Bundle","type":"document",
  "entry":[
    {"resource":{"resourceType":"Patient","id":"p1","identifier":[{"system":"urn:oid:1.2.203.0.1","value":"800101/1234"}],"name":[{"family":"Novák","given":["Petr"]}],"gender":"male","birthDate":"1980-01-01"}},
    {"resource":{"resourceType":"Organization","id":"org1","name":"FN Praha"}},
    {"resource":{"resourceType":"Encounter","id":"enc1","status":"finished","class":{"code":"IMP"},"period":{"start":"2024-12-28T09:00:00Z","end":"2025-01-01T09:30:00Z"}}},
    {"resource":{"resourceType":"Practitioner","id":"pr1","name":[{"family":"Lékař","given":["Jan"]}]}}
    ,
    {"resource":{
      "resourceType":"Composition",
      "meta":{"profile":["https://hl7.cz/fhir/hdr/StructureDefinition/cz-composition-hdr"]},
      "status":"final",
      "type":{"text":"CZ HDR"},
      "category":[{"text":"Discharge Report"}],
      "date":"2025-01-01T10:00:00Z",
      "title":"Hospital Discharge Report",
      "subject":{"reference":"Patient/p1"},
      "encounter":{"reference":"Encounter/enc1"},
      "author":[{"reference":"Practitioner/pr1"}],
      "attester":[{"mode":"legal","time":"2025-01-01T10:05:00Z","party":{"reference":"Practitioner/pr1"}}],
      "custodian":{"reference":"Organization/org1"},
      "section":[
        {"title":"A.2.2 Urgent Information","text":{"status":"generated","div":"<div>Život ohrožující alergie na penicilin</div>" }},
        {"title":"A.2.3 Hospitalization Data","text":{"status":"generated","div":"<div>Důvod přijetí, datum a umístění, právní status, dispozice po propuštění</div>" }},
        {"title":"A.2.7 Course of Hospitalization","text":{"status":"generated","div":"<div>Průběh hospitalizace a provedené významné výkony</div>" }},
        {"title":"A.2.8 Condition at Discharge","text":{"status":"generated","div":"<div>Stav při propuštění a doporučení</div>" }}
      ]
    }}
  ]
}
""";

    public static string BadMissingSections() => """
{ "resourceType":"Bundle","type":"document","entry":[
  { "resource":{"resourceType":"Patient","id":"p1"} },
  { "resource":{"resourceType":"Composition","status":"final","date":"2025-01-01T10:00:00Z",
    "subject":{"reference":"Patient/p1"},
    "author":[{"reference":"Practitioner/pr1"}],
    "attester":[{"mode":"legal","time":"2025-01-01T10:05:00Z","party":{"reference":"Practitioner/pr1"}}],
    "section":[{"title":"A.2.3 Hospitalization Data"}] } }
]}
""";
}