# Code-First Project Rename via Deploy Sync

**Date**: 2026-04-13
**Status**: Approved

## Problem

Projects have a `name` field in both the database (`Project.Name`) and in `compozerr.json`. Currently, the name is only set at project creation time. If a developer renames their project in `compozerr.json`, the backend never picks up the change.

## Design Decision

`compozerr.json` is the source of truth for the project name. The backend syncs from code, not the other way around.

## Solution

Add a "report config" callback during the post-deploy phase on the hosting manager, mirroring the existing service reporting pattern.

### Changes

**Hosting manager** (`manager/`):
- After reading `compozerr.json` (already happens in `compozerr-config-service.ts`), call a new backend endpoint to report the project name.
- New method on `DeploymentApiClient`: `reportProjectConfigAsync(projectId, { name })`.
- Called from `deployment-queue-service.ts` after the deploy completes, similar to how `ServiceReportingService.enqueueServiceReporting()` is called.

**Web backend** (`backend/Api.Hosting/`):
- New endpoint: `PUT /projects/{projectId}/config` accepting `{ name: string }`.
- New MediatR command + handler with FluentValidation (name required, max 255 chars).
- Handler updates `Project.Name` if the reported name differs.

### Subdomain Behavior

- Existing environment subdomains are stored at creation time and never change (approach C).
- New environments created after a rename use the updated `Project.Name` for subdomain generation.
- No migration needed.

### Extensibility

The `/config` endpoint can accept additional fields from `compozerr.json` in the future (e.g., `templateType`) without structural changes.
