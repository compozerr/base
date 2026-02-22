# Module Marketplace Design

**Date**: 2026-02-22
**Status**: Approved

## Overview

A full-stack module marketplace: backend registry with search API, public browseable marketplace pages (SEO-optimized), and a dashboard modules management page.

## Data Model

### ModuleRegistryEntry (new table, schema: api)

| Field | Type | Notes |
|-------|------|-------|
| Id | ModuleRegistryEntryId | PK |
| Name | string(255) | Module name, e.g. "stripe" |
| Organization | string(255) | GitHub org, e.g. "compozerr" |
| Description | string(2000) | From compozerr.json |
| RepoUrl | Uri | Public GitHub repo URL |
| LatestVersion | string(50) | Semver |
| Tags | string[] | PostgreSQL text array |
| InstallCount | int | Incremented on module add |
| Compatibility | jsonb | { dotnet: ">=9.0", node: ">=18" } |
| IconUrl | string? | Optional module icon |
| PublishedByUserId | UserId | FK |
| CreatedAtUtc | DateTime | |
| UpdatedAtUtc | DateTime | |

### ModuleVersion (new table, schema: api)

| Field | Type | Notes |
|-------|------|-------|
| Id | ModuleVersionId | PK |
| ModuleRegistryEntryId | FK | Parent module |
| Version | string(50) | Semver |
| CommitHash | string(40) | Git SHA |
| ReleaseNotes | string? | Markdown |
| CreatedAtUtc | DateTime | |

## Backend Endpoints

1. **GET /v1/modules?q=&tags=&limit=20&offset=0** — Search/browse. Full-text on name+description+tags. Shared by CLI and frontend.
2. **GET /v1/modules/{org}/{name}** — Detail view with versions, deps, compatibility.
3. **POST /v1/modules/release** — Upsert registry entry + create version. Auth required.
4. **POST /v1/modules/{org}/{name}/track-install** — Increment install count.

## Frontend Pages

### Public Marketplace (/modules)
- Hero section with search bar
- Category/tag filtering
- Module cards grid: name, org, description, tags, install count, latest version
- SEO: SSR-friendly meta tags, semantic HTML, structured data
- No auth required

### Module Detail (/modules/{org}/{name})
- Full description, install command snippet
- Version history with release notes
- Tags, compatibility badges
- Dependencies list
- Install count, published date
- SEO optimized with og:tags

### Dashboard My Modules (/dashboard/modules)
- User's modules across projects
- Status indicators (up to date, update available, modified)
- Quick actions (update, release, view in marketplace)
- Fits existing dashboard design language

## Search Engine
- Single backend endpoint used by both CLI (`compozerr module search`) and frontend
- PostgreSQL full-text search on name + description + tags
- Sorted by relevance score + install count
- Tag-based filtering
