# Wedding

> A free mobile invitation service combining maps, galleries, guestbook, account details, and music behind one shareable link.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://wedding.codemaru.co.kr/) · [User guide](https://codemaru.co.kr/guide/wedding) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

A free mobile invitation service combining maps, galleries, guestbook, account details, and music behind one shareable link.

Provides invitation authoring and management, public invitation pages, media uploads, and guestbook workflows.

## Key features

- OpenStreetMap with Kakao/Naver directions
- Photo gallery, video, and background music
- Guestbook with CSV administration
- Copyable account and KakaoPay links
- Open Graph previews for social sharing

## How to use

1. Sign in and enter names, date, and venue.
2. Add photos, music, video, and account information.
3. Review the theme and invitation copy.
4. Share the generated link through messaging or social media.

## Project information

| Item | Value |
|---|---|
| Project | WeddingPlatform.Web |
| Version | 1.0.0.0 |
| Target framework | net8.0-windows |
| Project file | WeddingPlatform.Web.csproj |

## Run for development

```powershell
dotnet run --project "WeddingPlatform.Web.csproj"
```

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
