# CodeMaru · CardHybrid

> A digital business-card service combining QR codes, mobile landing pages, vCard contact export, and card design in one workflow.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://codemaru.co.kr/cardhybrid) · [User guide](https://codemaru.co.kr/guide/cardhybrid) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

A digital business-card service combining QR codes, mobile landing pages, vCard contact export, and card design in one workflow.

Hosts the CodeMaru service hub, CardHybrid editor, public landing pages, authentication, and saved-card workflows.

## Key features

- Real-time SVG QR code and mobile landing-page generation
- Downloadable vCard contacts
- Front/back card layout, color, and font editing
- AI logo background removal and SVG/HTML export
- Authenticated card version history and restore

## How to use

1. Open CardHybrid and enter identity and contact details.
2. Customize brand colors, logo, and both card faces.
3. Verify the live QR code and mobile landing page.
4. Export SVG/HTML or share the QR link.
5. Signed-in users can save and restore versions.

## Project information

| Item | Value |
|---|---|
| Project | Codemaru |
| Version | 1.0.0.0 |
| Target framework | net8.0-windows |
| Project file | Codemaru.csproj |

## Run for development

```powershell
dotnet run --project "Codemaru.csproj"
```

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
