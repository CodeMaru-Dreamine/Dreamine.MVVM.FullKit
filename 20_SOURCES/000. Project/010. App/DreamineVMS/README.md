# DreamineVMS Agent

> A Windows desktop agent that converts RTSP or USB-camera streams to HLS for the remote CCTV Viewer.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://cctvviewer.codemaru.co.kr/) · [User guide](https://codemaru.co.kr/guide/cctv) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

A Windows desktop agent that converts RTSP or USB-camera streams to HLS for the remote CCTV Viewer.

Handles camera connectivity, FFmpeg transcoding, account/device registration, and stream state management.

## Key features

- RTSP and USB-camera connectivity
- FFmpeg-based HLS conversion
- Multiple cameras and display ordering
- Automatic reconnect and stream health
- Account-based remote server registration

## How to use

1. Run the agent on Windows 10 or later.
2. Register the device with CCTV Viewer credentials.
3. Add an RTSP URL or connected camera.
4. Verify the live stream in the web viewer.

## Project information

| Item | Value |
|---|---|
| Project | DreamineVMS |
| Version | 1.0.0.0 |
| Target framework | net8.0-windows |
| Project file | DreamineVMS.csproj |

## Run for development

```powershell
dotnet run --project "DreamineVMS.csproj"
```

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
