
# NCEZ.Simulator (ASP.NET Core, .NET 9)

Simulátor vybraných rozhraní EZ pro vývoj a testování. Obsahuje REST API, perzistenci do JSON souborů a Swagger UI na `/`.

## Spuštění
```bash
dotnet build src/NCEZ.Simulator/NCEZ.Simulator.csproj
dotnet run --project src/NCEZ.Simulator/NCEZ.Simulator.csproj
```
Swagger poběží na `http://localhost:5000` resp. `https://localhost:5001` (podle nastavení).

## Přehled domén a endpointů
- **Notifikační služby** `/api/v1/notifications`
  - `POST /publish`, `GET /`, `GET /{id}`, `POST /{id}/ack`, `DELETE /{id}`
- **Registr oprávnění** `/api/v1/authorization`
  - `POST /grants`, `GET /grants`, `GET /grants/{id}`, `PUT /grants/{id}`, `DELETE /grants/{id}`
- **Žurnál činností** `/api/v1/journal`
  - `POST /entries`, `GET /entries`, `GET /entries/{id}`
- **Dočasné úložiště** `/api/v1/temp-storage`
  - `POST /documents` (multipart/form-data), `GET /documents`, `GET /documents/{id}`, `GET /documents/{id}/content`, `DELETE /documents/{id}`
- **ePosudky** `/api/v1/eposudky`
  - `POST /`, `GET /`, `GET /{id}`
- **EZKarta** `/api/v1/ezkarta`
  - `POST /`, `GET /`, `GET /{id}`
- **eŽádanky** `/api/v1/ezadanky`
  - `POST /`, `GET /`, `GET /{id}`, `POST /{id}/status`
- **Katalog služeb EZ** `/api/v1/catalog`
  - `POST /services`, `GET /services`, `GET /services/{id}`
- **Kmenové registry** `/api/v1/registers`
  - `POST|GET patients/practitioners/providers`, `GET {entity}/{id}`
- **Sdílený zdravotní záznam (FHIR R4)** `/api/v1/fhir/{resourceType}`
  - `POST`, `GET`, `GET /{id}` (minimální obálka resource)
- **Afinitní domény (XDS.b simulace)** `/api/v1/affinity`
  - `POST /provide-and-register`, `GET /entries`, `GET /entries/{id}`
- **Testovací rámec** `/api/v1/test`
  - `POST /seed?count=20`, `GET /status`
  
### Rychlý start s testovacími daty
curl -X POST "http://localhost:5000/api/v1/test/seed?count=20"

## Perzistence
Číselníky a mapy konceptů jsou read-only a očekávají se v `Data/CodeSets` a `Data/CodeSets/ConceptMaps`.

## Číselníky
Číselníky se načítají při startu z adresáře `Data/CodeSets` a mapy konceptů z `Data/CodeSets/ConceptMaps`.

### Dodané datové sady (syntetické pro účely testů)
- **ICD-10**: `Data/CodeSets/icd10@2024.json` — ~150 položek (např. I10, E11, J06.9, A09)
- **SNOMED CT (syntetický výčet)**: `Data/CodeSets/snomed_extended.json` — ~650 položek
- **LOINC (syntetický výčet)**: `Data/CodeSets/loinc_extended.json` — ~550 položek
- **UCUM jednotky**: `Data/CodeSets/ucum.json` — 100+ položek
- **ConceptMap ICD-10 → SNOMED**: `Data/CodeSets/ConceptMaps/icd10-to-snomed.json` — reálné dvojice pro I10, E11, J06.9, A09 mají vztah `equivalent`, ostatní jsou označeny `related`.

> Poznámka: SNOMED/LOINC zde nejsou oficiální úplné licencované sady. Slouží jen k syntetickému testování.

### Rozšíření číselníků
- Pro nahrazení nebo rozšíření stačí upravit JSON soubory nebo doplnit vlastní importér v `CodeSetService`.
- Simulator při startu validuje základní strukturu JSON (kód + popis / zobrazení).

## Poznámky
- Cílem je simulátor. Nejedná se o produkční implementaci standardů EZ. 
- Validace vrací `422 Unprocessable Entity` s `ValidationProblemDetails`.
- Přidejte autentizaci dle potřeby (viz `Simulator.RequireApiKey`).
