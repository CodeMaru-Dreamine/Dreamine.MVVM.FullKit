# BabyCare / 아기하루

[![CI](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit/actions/workflows/ci.yml)

.NET 8 Blazor Server family care journal with shared family authorization and SQLite persistence.

- Feeding: formula, nursing side and duration, expressed milk; solids in grams with food notes.
- Burping, sleep, diapers and color observations, temperature, and left/right pumping records.
- Birth date dashboard, caregiver-defined goals, reviewed sleep suggestions, profile and record photos.
- Ten UI/input languages, translated AI notes, collapsible input examples, light/dark themes.
- Commands become editable drafts. Saving requires explicit caregiver confirmation. Manual input remains available when AI fails.

## Development and verification

Initialize repository submodules, then run from the repository root:

```powershell
dotnet test "20_SOURCES/200. Tests/BabyCare.Web.Tests/BabyCare.Web.Tests.csproj" -c Release
dotnet run --project "20_SOURCES/000. Project/010. App/BabyCare.Web/BabyCare.Web.csproj"
```

The default configuration consumes the central CodeMaru authentication cookie. For an isolated environment, configure authentication, data and key paths separately; never reuse production storage for tests. Codex execution requires a separately configured executable and authenticated service account. CI does not require a live Codex account.

## Storage and deployment

Configure `BabyCare:DataPath` for persistent data. Photos live below its `photos` directory, outside `wwwroot`, and require family membership. Back up the database, photos and authentication keys together. Photo limits: one profile photo, three per record, 8 MB each, JPEG/PNG/WebP. Detaching a photo removes its record reference; physical orphan cleanup is not implemented. Uploaded originals retain metadata.

Published packages and coordination files are local artifacts, not source files. Deployments are manual; this repository's CI does not restart services. Do not replace operating data or keys during runtime updates.

Age-based sleep information is reference material, not an individual prescription. Caregiver goals and unconfirmed sleep estimates are presented separately from actual observations. Nursing time is never converted to milk volume; solids and pumping are not added to infant milk intake.
