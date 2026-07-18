# CCTV Viewer

> A remote CCTV web service for managing and playing HLS camera streams supplied by DreamineVMS agents.

![.NET](https://img.shields.io/badge/.NET-net8.0-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://cctvviewer.codemaru.co.kr/) · [User guide](https://codemaru.co.kr/guide/cctv) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

A remote CCTV web service for managing and playing HLS camera streams supplied by DreamineVMS agents.

Provides authentication, device and camera management, live viewing, public links, and Open Graph metadata.

## Key features

- Browser-based live HLS playback
- Per-account devices and multiple cameras
- Public live links without sign-in
- Per-camera Open Graph title, description, and image
- PBKDF2 authentication and persistent sessions

## How to use

1. Create a service account.
2. Connect a DreamineVMS agent on the camera PC.
3. Register streams in camera management.
4. Watch through live view or a public link.

## Project information

| Item | Value |
|---|---|
| Project | DreamineVMS.Web |
| Version | 1.0.0.0 |
| Target framework | net8.0 |
| Project file | DreamineVMS.Web.csproj |

## Run for development

```powershell
dotnet run --project "DreamineVMS.Web.csproj"
```

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
