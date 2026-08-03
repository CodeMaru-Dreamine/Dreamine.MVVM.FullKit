# Families

> A private family album and timeline for sharing photos, videos, stories, comments, and reactions.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[Open service](https://families.codemaru.co.kr/) · [User guide](https://codemaru.co.kr/guide/families) · [GitHub](https://github.com/CodeMaru-Dreamine)

## Overview

A private family album and timeline for sharing photos, videos, stories, comments, and reactions.

Provides family-group access, private posts and albums, comments, reactions, and media delivery.

## Key features

- Password-protected private groups
- Photo, video, YouTube, and Markdown posts
- Event-oriented album folders
- Pinned posts, comments, and emoji reactions
- Light/dark themes and group covers

## How to use

1. Create a family group and set its password.
2. Share the group link and password with family.
3. Create posts or albums for media and stories.
4. Continue the family record through comments and reactions.

## Project information

| Item | Value |
|---|---|
| Project | Families.Web |
| Version | 1.0.0.0 |
| Target framework | net8.0-windows |
| Project file | Families.Web.csproj |

## Run for development

```powershell
$env:Family__SuperAdminPassword = "a-strong-local-only-password"
dotnet run --project "Families.Web.csproj"
```

## Administrator access

- Family album administrator: `/{slug}/admin` — sign in with the password chosen when the album was created or a linked CodeMaru account.
- Super administrator: `/admin` — use the password supplied through the `Family__SuperAdminPassword` environment variable.
- The repository intentionally ships without sample albums or a default super-administrator password. Keep the production password in the deployment platform's secret settings rather than source control or `appsettings.json`.

## Deployment checks for mobile uploads

- The application accepts a video up to 2 GiB and reserves another 32 MiB for multipart overhead. Configure every upstream layer (IIS Request Filtering, nginx, a CDN, ingress, or another reverse proxy) with a request-body limit at least as large as the active tenant policy plus multipart overhead.
- Keep proxy request/send/read timeouts at 35 minutes or longer. A proxy-side `413 Payload Too Large` or timeout occurs before the Families upload endpoint runs and commonly appears only with large videos recorded on phones.
- Upload tickets are one-use capabilities held in application memory. When more than one Families instance serves the same host, use sticky sessions for ticket creation and upload, or replace the in-memory ticket store with a shared distributed implementation.
- Viewer-password throttling uses the resolved remote address. At the edge, accept forwarded-client headers only from explicitly trusted proxies, have that proxy overwrite/remove any client-supplied `X-Forwarded-For` value before adding its own, and block direct Kestrel/origin access; otherwise a forged forwarded address can weaken per-client throttling.

## Private album access

- New albums use separate administrator and family viewing passwords. Share only the viewing password with visitors.
- Existing album JSON without `ViewerPasswordHash` temporarily falls back to the administrator password. Set a separate viewing password at `/{slug}/admin` and save settings to migrate it.
- Visitor access uses a signed, tenant-bound, HTTP-only cookie valid for 24 hours. The same grant protects images and videos under `/family-data/{slug}` and cannot be reused for another slug.
- Persist and share ASP.NET Core Data Protection keys in a multi-instance deployment so every instance can validate the same visitor grants.

## Generate API documentation

```powershell
doxygen Doxyfile.en
```
Generate the Korean documentation with `Doxyfile.kr`.
