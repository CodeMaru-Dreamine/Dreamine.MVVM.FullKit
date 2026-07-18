# Families AutoWriter

> An authoring automation tool that helps prepare and publish Families content without repetitive manual work.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4)- ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://families.codemaru.co.kr/) · [User guide](https://codemaru.co.kr/guide/families) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

An authoring automation tool that helps prepare and publish Families content without repetitive manual work.

An operator-facing authoring companion for preparing posts and media consumed by Families.Web.

## Key features

- Family-content draft authoring
- Automated media and body entry
- Families.Web integration
- Reduced repetitive operator work

## How to use

1. Verify the target Families environment.
2. Prepare post text and media.
3. Review the generated authoring result.
4. Confirm final publication in Families.Web.

## Project information

| Item | Value |
|---|---|
| Project | Families.AutoWriter |
| Version | 1.0.0.0 |
| Target framework | net8.0-windows |
| Project file | Families.AutoWriter.csproj |

## Run for development

```powershell
dotnet run --project "Families.AutoWriter.csproj"
```

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
