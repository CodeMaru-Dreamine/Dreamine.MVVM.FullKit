# Dreamine

> The official web application for the open-source FullKit that connects WPF and Blazor workflows while reducing repetitive MVVM code.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://dreamine.kr/) · [User guide](https://codemaru.co.kr/guide/dreamine) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

The official web application for the open-source FullKit that connects WPF and Blazor workflows while reducing repetitive MVVM code.

Provides the FullKit portal, package and example discovery, knowledge graph, and developer documentation.

## Key features

- FullKit package and layer overview
- Beginner recipes and troubleshooting
- Project-level API and Doxygen entry points
- Whole-solution knowledge graph
- Official GitHub and NuGet links

## How to use

1. Read the five-minute quick start.
2. Choose a WPF, communication, PLC, or Hybrid recipe.
3. Inspect class and method contracts in the API section.
4. Open the knowledge graph when architectural context is needed.

## Project information

| Item | Value |
|---|---|
| Project | Dreamine.Web |
| Version | 1.0.0.0 |
| Target framework | net8.0-windows |
| Project file | Dreamine.Web.csproj |

## Run for development

```powershell
dotnet run --project "Dreamine.Web.csproj"
```

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
