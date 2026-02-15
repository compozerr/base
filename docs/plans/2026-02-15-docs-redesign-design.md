# Docs Redesign Design

**Date**: 2026-02-15
**Status**: Approved

## Goal

Redesign the Compozerr docs module to be tutorial-first with complete reference documentation. Tutorials serve as the primary learning path with "Read more" links to deep reference pages. Keep Docusaurus but restyle with distinctive Compozerr branding.

## Audience

Two audiences, clearly separated:
1. **Developers deploying existing apps** (any stack) via Compozerr CLI
2. **Full-stack developers** building on the Compozerr template (.NET + React)

## Decisions

- **Platform**: Keep Docusaurus 3.x, restyle with custom Compozerr branding
- **Visual direction**: Delegated to frontend-design skill (dark mode primary, developer-focused, distinct from default Docusaurus)
- **Examples**: Generic examples only (no references to real personal projects)
- **Navigation**: Tutorial-first sidebar with collapsible reference section

## Information Architecture

```
/ (Landing page - hero + value props + quick links)
│
├── Getting Started
│   ├── Installation
│   └── Quick Start
│
├── Tutorials                        ← PRIMARY PATH
│   ├── Deploy a Docker Compose Project
│   ├── Deploy a Next.js App
│   ├── Deploy a Static Website
│   ├── Build a Full-Stack App (Compozerr Template)
│   ├── Create & Publish a Module
│   ├── Set Up Custom Domains & SSL
│   ├── SSH Into Your Project
│   └── Monitor Deployments
│
├── Reference                        ← DEEP DOCS
│   ├── CLI Commands
│   │   ├── login / logout / me
│   │   ├── new (project, module)
│   │   ├── deploy
│   │   ├── deployment (status, logs, list, latest)
│   │   ├── dev
│   │   ├── migrate
│   │   ├── ssh
│   │   ├── add / remove
│   │   ├── tiers
│   │   ├── cleanup
│   │   └── update / reset / global flags
│   ├── Configuration
│   │   ├── compozerr.json
│   │   └── Environment Variables
│   └── Architecture
│       ├── How Deployment Works
│       ├── Module System
│       ├── Frontend Architecture
│       └── Project Structure
│
└── Troubleshooting
    ├── Project Won't Start
    ├── Deployment Failures
    ├── Domain & SSL Issues
    └── Common Errors
```

## Tutorial Structure Pattern

Every tutorial follows:
1. What you'll build/achieve (1 sentence + expected outcome)
2. Prerequisites (callout box)
3. Step-by-step instructions (numbered, with code blocks)
4. "Read more" callout boxes inline linking to reference pages
5. What's next (link to logical next tutorial)

## CLI Reference Page Pattern

Every CLI command page follows:
1. Synopsis (command + all flags in one-liner)
2. Description (what it does, 2-3 sentences)
3. Arguments & Flags (table: name, type, default, description)
4. Examples (3-4 real usage examples with output)
5. Related Commands (links)

## Custom MDX Components

### `<ReadMore />`
Styled card with left border accent. Links tutorial content to reference pages.

### `<Prerequisites />`
Checklist box at top of tutorials showing what's needed before starting.

### `<CommandBlock />`
Enhanced code block showing CLI command + expected output. Copy button copies only the command.

### `<FlagTable />`
Structured table for CLI flags/arguments with columns: Flag, Type, Default, Description.

### `<WhatNext />`
Tutorial footer with 2 suggested next pages and short descriptions.

## Landing Page

- Hero: "Deploy anything. From code to cloud in one command."
- Three value props: Any Stack, Auto SSL, Module System
- Quick start code block (4 commands)
- Links to Getting Started and Tutorials

## Content Inventory

### Getting Started (2 pages)
- **Installation**: npm install, verify, prerequisites
- **Quick Start**: install → login → new project → dev → deploy (5 min)

### Tutorials (8 pages)
- Deploy a Docker Compose Project (migrate flow, compozerr.json config)
- Deploy a Next.js App (migrate, Dockerfile, standalone mode)
- Deploy a Static Website (migrate, Nginx pattern)
- Build a Full-Stack App (new project, template tour, dev, deploy)
- Create & Publish a Module (new module, IFeature, endpoint, frontend route)
- Set Up Custom Domains & SSL (dashboard, DNS, SSL auto-provisioning)
- SSH Into Your Project (interactive + one-off commands)
- Monitor Deployments (deployment status/logs/list/latest)

### Reference — CLI Commands (13 pages)
All commands with full flag documentation: login, logout, me, new, deploy, deployment, dev, migrate, ssh, add/remove, tiers, cleanup, update/reset + global flags.

### Reference — Configuration (2 pages)
- compozerr.json full schema (project + module types)
- Environment Variables (flow, locations, build-time vs runtime)

### Reference — Architecture (4 pages)
- How Deployment Works (conceptual pipeline)
- Module System (IFeature, MediatR, Carter)
- Frontend Architecture (TanStack Router, Qraft)
- Project Structure (annotated directory tree)

### Troubleshooting (4 pages)
- Project Won't Start
- Deployment Failures
- Domain & SSL Issues
- Common Errors

**Total: ~33 pages**
