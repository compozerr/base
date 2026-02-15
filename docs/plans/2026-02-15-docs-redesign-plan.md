# Docs Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign the Compozerr docs module with tutorial-first content, complete CLI reference, custom Compozerr branding, and custom MDX components.

**Architecture:** Docusaurus 3.x with custom CSS theming, MDX components for reusable patterns (ReadMore, Prerequisites, CommandBlock, FlagTable, WhatNext), manual sidebar configuration, and a custom landing page. Content organized as Getting Started > Tutorials > Reference > Troubleshooting.

**Tech Stack:** Docusaurus 3.9.2, React 19, TypeScript, MDX, CSS Modules, Infima CSS framework

**Design doc:** `docs/plans/2026-02-15-docs-redesign-design.md`

**Base path:** `modules/docs/frontend` (all paths below are relative to this)

---

## Task 1: Clean Up Placeholder Content & Update Config

Remove all default Docusaurus placeholder content and configure for Compozerr.

**Files:**
- Modify: `docusaurus.config.ts`
- Delete: `src/pages/markdown-page.md`
- Delete: `blog/` directory (entire folder)
- Delete: `static/img/undraw_docusaurus_mountain.svg`
- Delete: `static/img/undraw_docusaurus_tree.svg`
- Delete: `static/img/undraw_docusaurus_react.svg`
- Delete: `static/img/docusaurus.png`
- Delete: `static/img/docusaurus-social-card.jpg`
- Delete: `static/img/logo.svg`
- Delete: `docs/intro.md`
- Delete: `docs/quick-start.md`
- Delete: `docs/basics/` (entire directory)
- Delete: `docs/troubleshoot/` (entire directory)

**Step 1: Update `docusaurus.config.ts`**

Replace the full config with:

```typescript
import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Compozerr',
  tagline: 'Deploy anything. From code to cloud in one command.',
  favicon: 'img/favicon.ico',
  url: 'https://docs.compozerr.com',
  baseUrl: '/',
  organizationName: 'compozerr',
  projectName: 'compozerr',
  onBrokenLinks: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],
  themeConfig: {
    colorMode: {
      disableSwitch: true,
      defaultMode: 'dark',
      respectPrefersColorScheme: false,
    },
    navbar: {
      title: 'compozerr',
      style: 'dark',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'mainSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/compozerr',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://compozerr.com',
          label: 'Dashboard',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Learn',
          items: [
            { label: 'Getting Started', to: '/getting-started/installation' },
            { label: 'Tutorials', to: '/tutorials/deploy-docker-compose' },
          ],
        },
        {
          title: 'Reference',
          items: [
            { label: 'CLI Commands', to: '/reference/cli/deploy' },
            { label: 'Configuration', to: '/reference/configuration/compozerr-json' },
          ],
        },
        {
          title: 'More',
          items: [
            { label: 'GitHub', href: 'https://github.com/compozerr' },
            { label: 'Dashboard', href: 'https://compozerr.com' },
          ],
        },
      ],
      copyright: `Copyright \u00a9 ${new Date().getFullYear()} Compozerr`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['bash', 'json', 'csharp', 'docker', 'yaml', 'nginx'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
```

**Step 2: Delete all placeholder files**

```bash
cd modules/docs/frontend
rm -f src/pages/markdown-page.md
rm -rf blog/
rm -f static/img/undraw_docusaurus_mountain.svg
rm -f static/img/undraw_docusaurus_tree.svg
rm -f static/img/undraw_docusaurus_react.svg
rm -f static/img/docusaurus.png
rm -f static/img/docusaurus-social-card.jpg
rm -f static/img/logo.svg
rm -rf docs/basics/
rm -rf docs/troubleshoot/
rm -f docs/intro.md
rm -f docs/quick-start.md
```

**Step 3: Commit**

```bash
git add -A modules/docs/frontend
git commit -m "chore(docs): remove placeholder content and update config"
```

---

## Task 2: Create Sidebar Configuration

Replace auto-generated sidebar with manual configuration matching the information architecture.

**Files:**
- Modify: `sidebars.ts`

**Step 1: Write sidebar config**

Replace `sidebars.ts` with:

```typescript
import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  mainSidebar: [
    {
      type: 'category',
      label: 'Getting Started',
      collapsed: false,
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
      ],
    },
    {
      type: 'category',
      label: 'Tutorials',
      collapsed: false,
      items: [
        'tutorials/deploy-docker-compose',
        'tutorials/deploy-nextjs',
        'tutorials/deploy-static-website',
        'tutorials/build-fullstack-app',
        'tutorials/create-publish-module',
        'tutorials/custom-domains-ssl',
        'tutorials/ssh-into-project',
        'tutorials/monitor-deployments',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'CLI Commands',
          collapsed: true,
          items: [
            'reference/cli/login',
            'reference/cli/new',
            'reference/cli/deploy',
            'reference/cli/deployment',
            'reference/cli/dev',
            'reference/cli/migrate',
            'reference/cli/ssh',
            'reference/cli/add-remove',
            'reference/cli/tiers',
            'reference/cli/cleanup',
            'reference/cli/update-reset',
          ],
        },
        {
          type: 'category',
          label: 'Configuration',
          collapsed: true,
          items: [
            'reference/configuration/compozerr-json',
            'reference/configuration/environment-variables',
          ],
        },
        {
          type: 'category',
          label: 'Architecture',
          collapsed: true,
          items: [
            'reference/architecture/how-deployment-works',
            'reference/architecture/module-system',
            'reference/architecture/frontend-architecture',
            'reference/architecture/project-structure',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'Troubleshooting',
      collapsed: true,
      items: [
        'troubleshooting/project-wont-start',
        'troubleshooting/deployment-failures',
        'troubleshooting/domain-ssl-issues',
        'troubleshooting/common-errors',
      ],
    },
  ],
};

export default sidebars;
```

**Step 2: Create empty directory structure**

```bash
cd modules/docs/frontend/docs
mkdir -p getting-started tutorials reference/cli reference/configuration reference/architecture troubleshooting
```

**Step 3: Commit**

```bash
git add sidebars.ts docs/
git commit -m "chore(docs): configure manual sidebar with full information architecture"
```

---

## Task 3: Design & Apply Custom Theme

Use the `frontend-design` skill to create a distinctive Compozerr visual identity, then apply it to the Docusaurus CSS.

**Files:**
- Modify: `src/css/custom.css`
- Modify: `src/components/HomepageFeatures/index.tsx`
- Modify: `src/components/HomepageFeatures/styles.module.css`

**Step 1: Invoke `frontend-design` skill**

Design a dark-mode docs theme for Compozerr with:
- Distinctive accent color (not default Docusaurus blue)
- Custom code block styling (critical - most content is CLI examples)
- Sidebar styling that differentiates Tutorials from Reference
- Clean, developer-focused aesthetic
- Landing page hero with gradient or subtle animation

**Step 2: Apply the designed CSS to `src/css/custom.css`**

The frontend-design skill will produce the full CSS. Replace `custom.css` entirely.

**Step 3: Redesign `HomepageFeatures` component**

Replace with Compozerr-specific value propositions:
- "Any Stack" - Deploy Docker, Node, .NET, static sites, or anything with a Dockerfile
- "Auto SSL" - Custom domains with automatic HTTPS via Let's Encrypt
- "Module System" - Plug-and-play full-stack modules for auth, payments, databases

Use CSS-based icons (no SVG files needed) or simple emoji/unicode icons.

**Step 4: Verify the dev server runs**

```bash
cd modules/docs/frontend && npm run dev
```

Open http://localhost:1236 and verify the theme looks correct.

**Step 5: Commit**

```bash
git add src/css/custom.css src/components/
git commit -m "feat(docs): apply custom Compozerr theme and branding"
```

---

## Task 4: Build Custom MDX Components

Create the 5 reusable MDX components defined in the design.

**Files:**
- Create: `src/components/ReadMore/index.tsx`
- Create: `src/components/ReadMore/styles.module.css`
- Create: `src/components/Prerequisites/index.tsx`
- Create: `src/components/Prerequisites/styles.module.css`
- Create: `src/components/CommandBlock/index.tsx`
- Create: `src/components/CommandBlock/styles.module.css`
- Create: `src/components/FlagTable/index.tsx`
- Create: `src/components/FlagTable/styles.module.css`
- Create: `src/components/WhatNext/index.tsx`
- Create: `src/components/WhatNext/styles.module.css`

### ReadMore Component

Props: `to: string`, `title: string`, `description: string`

Renders a styled card with left accent border, title, description, and "Read more →" link.

Usage in MDX:
```mdx
import ReadMore from '@site/src/components/ReadMore';

<ReadMore
  to="/reference/cli/deploy"
  title="deploy command reference"
  description="Full flag reference, CI/CD usage, and advanced options."
/>
```

### Prerequisites Component

Props: `items: string[]`

Renders a callout box with checkmark list.

Usage in MDX:
```mdx
import Prerequisites from '@site/src/components/Prerequisites';

<Prerequisites items={[
  "Compozerr CLI installed",
  "GitHub account connected",
  "A Docker Compose project"
]} />
```

### CommandBlock Component

Props: `command: string`, `output?: string`

Renders a terminal-style code block with the command and optional output. Copy button copies only the command.

Usage in MDX:
```mdx
import CommandBlock from '@site/src/components/CommandBlock';

<CommandBlock
  command="compozerr deploy"
  output={`Deploying project...\n✓ Build completed\n✓ Services started\n✓ Domain configured`}
/>
```

### FlagTable Component

Props: `flags: Array<{ name: string, type: string, default?: string, description: string }>`

Renders a styled table of CLI flags.

Usage in MDX:
```mdx
import FlagTable from '@site/src/components/FlagTable';

<FlagTable flags={[
  { name: "--wait", type: "boolean", default: "false", description: "Wait for deployment to complete" },
  { name: "--json", type: "boolean", default: "false", description: "Output results as JSON" },
]} />
```

### WhatNext Component

Props: `items: Array<{ title: string, to: string, description: string }>`

Renders a footer section with suggested next pages.

Usage in MDX:
```mdx
import WhatNext from '@site/src/components/WhatNext';

<WhatNext items={[
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your project" },
  { title: "Monitor Deployments", to: "/tutorials/monitor-deployments", description: "Track deployment status and view logs" },
]} />
```

**Step: Verify all components render**

Create a temporary test page at `docs/component-test.md` that imports and renders all 5 components. Verify in browser. Delete after confirming.

**Step: Commit**

```bash
git add src/components/
git commit -m "feat(docs): add custom MDX components (ReadMore, Prerequisites, CommandBlock, FlagTable, WhatNext)"
```

---

## Task 5: Write Landing Page

Create the docs landing/intro page with hero section, value props, and quick start.

**Files:**
- Create: `docs/index.mdx`

**Content:**

The landing page should contain:
1. Hero section: "Deploy anything. From code to cloud in one command." with subtitle about Compozerr being a deployment platform
2. Three value prop cards (using HomepageFeatures or inline MDX):
   - Any Stack: Docker, Node, .NET, static sites
   - Auto SSL: Custom domains + HTTPS
   - Module System: Plug-and-play full-stack modules
3. Quick start code block:
   ```bash
   /bin/bash -c "$(curl -fsSL https://compozerr.com/install.sh)"
   compozerr login
   compozerr new project my-app
   compozerr deploy
   ```
4. Two CTA links: "Get Started →" and "Browse Tutorials →"

Use frontmatter: `slug: /`, `sidebar_position: 0`, `hide_table_of_contents: true`

**Step: Verify in browser**

```bash
cd modules/docs/frontend && npm run dev
```

**Step: Commit**

```bash
git add docs/index.mdx
git commit -m "feat(docs): add landing page with hero, value props, and quick start"
```

---

## Task 6: Write Getting Started Pages

**Files:**
- Create: `docs/getting-started/installation.md`
- Create: `docs/getting-started/quick-start.md`

### installation.md

Frontmatter: `sidebar_position: 1`

Content sections:
1. **Install the CLI** - curl command from compozerr.com/install.sh
2. **Verify installation** - `compozerr --version`
3. **Prerequisites** - Node.js >= 18, GitHub account
4. **Login** - `compozerr login` (opens browser for OAuth)
5. **Payment setup** - brief note that payment method needed for deployments, link to dashboard
6. ReadMore → `reference/cli/login`

### quick-start.md

Frontmatter: `sidebar_position: 2`

Content sections (numbered steps):
1. Install CLI (link to installation page)
2. Login: `compozerr login`
3. Create project: `compozerr new project my-app` with explanation of what gets generated
4. Start dev: `compozerr dev` or press F5 in IDE, explain frontend on 3001 and backend on 5000
5. Deploy: `compozerr deploy` with expected output
6. WhatNext → tutorials

Use CommandBlock component for each step. Use Prerequisites component at top.

**Step: Commit**

```bash
git add docs/getting-started/
git commit -m "feat(docs): add Getting Started pages (installation, quick start)"
```

---

## Task 7: Write Tutorials — Deployment Tutorials

These 3 tutorials cover deploying existing projects.

**Files:**
- Create: `docs/tutorials/deploy-docker-compose.md`
- Create: `docs/tutorials/deploy-nextjs.md`
- Create: `docs/tutorials/deploy-static-website.md`

### deploy-docker-compose.md

Frontmatter: `sidebar_position: 1`

Prerequisites: CLI installed, logged in, existing docker-compose project

Steps:
1. Navigate to project directory
2. Run `compozerr migrate` - explain the detection flow
3. Review generated `compozerr.json` - explain `templateType: "docker-compose"` and `templateFile`
4. Configure environment variables in dashboard
5. Run `compozerr deploy`
6. Verify deployment: `compozerr deployment status`

ReadMore → `reference/cli/migrate`, `reference/configuration/compozerr-json`
WhatNext → custom-domains-ssl, monitor-deployments

### deploy-nextjs.md

Frontmatter: `sidebar_position: 2`

Prerequisites: CLI installed, logged in, existing Next.js app

Steps:
1. Navigate to project directory
2. Run `compozerr migrate` - detects as docker-compose or dockerfile
3. Ensure Dockerfile exists (provide example multi-stage Next.js Dockerfile if needed)
4. Handle `NEXT_PUBLIC_*` env vars as build args in docker-compose
5. Run `compozerr deploy`
6. Verify

Example Dockerfile provided inline (multi-stage: deps → builder → runner with standalone output).

ReadMore → `reference/cli/migrate`, `reference/configuration/environment-variables`
WhatNext → custom-domains-ssl, ssh-into-project

### deploy-static-website.md

Frontmatter: `sidebar_position: 3`

Prerequisites: CLI installed, logged in, static site with build step

Steps:
1. Navigate to project directory
2. Create a Dockerfile (example: Node build → Nginx serve)
3. Create nginx.conf for SPA routing
4. Run `compozerr migrate`
5. Run `compozerr deploy`
6. Verify

ReadMore → `reference/cli/deploy`
WhatNext → custom-domains-ssl, monitor-deployments

**Step: Commit**

```bash
git add docs/tutorials/deploy-*.md
git commit -m "feat(docs): add deployment tutorials (Docker Compose, Next.js, static site)"
```

---

## Task 8: Write Tutorials — Build & Module Tutorials

**Files:**
- Create: `docs/tutorials/build-fullstack-app.md`
- Create: `docs/tutorials/create-publish-module.md`

### build-fullstack-app.md

Frontmatter: `sidebar_position: 4`

Prerequisites: CLI installed, logged in, .NET SDK 9.0, Node.js >= 18

Steps:
1. Create project: `compozerr new project my-app`
2. Tour the generated structure (annotated tree: backend/, frontend/, modules/, scripts/)
3. Start development: `compozerr dev` - explain what starts (frontend on 3001, backend on 5000)
4. Make a backend change: add a simple endpoint using Carter + MediatR pattern
5. Make a frontend change: consume the new endpoint using the generated API client
6. Regenerate API client: `npx openapi-qraft ...` command
7. Deploy: `compozerr deploy`

ReadMore → `reference/architecture/project-structure`, `reference/architecture/module-system`
WhatNext → create-publish-module, custom-domains-ssl

### create-publish-module.md

Frontmatter: `sidebar_position: 5`

Prerequisites: CLI installed, logged in, existing Compozerr project

Steps:
1. Create module: `compozerr new module my-module`
2. Tour the generated module structure
3. Implement IFeature in backend (register a service)
4. Add an endpoint using Carter
5. Add a command handler using MediatR
6. Add a frontend route (file-based routing)
7. Export a component via package.json exports
8. Test locally with `compozerr dev`
9. Deploy module: `compozerr deploy module my-module` (if applicable) or describe sharing workflow

ReadMore → `reference/architecture/module-system`, `reference/cli/new`
WhatNext → deploy-docker-compose, custom-domains-ssl

**Step: Commit**

```bash
git add docs/tutorials/build-fullstack-app.md docs/tutorials/create-publish-module.md
git commit -m "feat(docs): add full-stack app and module tutorials"
```

---

## Task 9: Write Tutorials — Operations Tutorials

**Files:**
- Create: `docs/tutorials/custom-domains-ssl.md`
- Create: `docs/tutorials/ssh-into-project.md`
- Create: `docs/tutorials/monitor-deployments.md`

### custom-domains-ssl.md

Frontmatter: `sidebar_position: 6`

Prerequisites: CLI installed, deployed project, domain you own

Steps:
1. Add domain in dashboard (brief - link to dashboard UI)
2. Configure DNS: A record or CNAME pointing to Compozerr
3. Wait for DNS propagation (explain TTL)
4. SSL is auto-provisioned via Let's Encrypt
5. Verify: visit https://yourdomain.com
6. Multiple domains: explain you can add multiple domains per service

ReadMore → `reference/architecture/how-deployment-works`
WhatNext → ssh-into-project, monitor-deployments

### ssh-into-project.md

Frontmatter: `sidebar_position: 7`

Prerequisites: CLI installed, logged in, deployed project

Steps:
1. Interactive session: `compozerr ssh` - explain what happens (WebSocket terminal to VM)
2. One-off command: `compozerr ssh -c "docker ps"` - non-interactive execution
3. Common debugging tasks:
   - Check running containers: `docker ps`
   - View container logs: `docker logs <container>`
   - Restart services: `docker compose restart`
   - Check disk space: `df -h`
4. Specifying project: `compozerr ssh <project-id>`

ReadMore → `reference/cli/ssh`
WhatNext → monitor-deployments, deploy-docker-compose

### monitor-deployments.md

Frontmatter: `sidebar_position: 8`

Prerequisites: CLI installed, logged in, at least one deployment

Steps:
1. Latest deployment: `compozerr deployment latest` - shows status + last 20 logs
2. Specific deployment: `compozerr deployment status <id>`
3. View logs: `compozerr deployment logs <id>` - explain log levels (info, error, warning, success)
4. List all: `compozerr deployment list` - with `--status` filter and `--page` pagination
5. JSON output: `compozerr deployment status --json` for scripting

ReadMore → `reference/cli/deployment`
WhatNext → ssh-into-project, custom-domains-ssl

**Step: Commit**

```bash
git add docs/tutorials/custom-domains-ssl.md docs/tutorials/ssh-into-project.md docs/tutorials/monitor-deployments.md
git commit -m "feat(docs): add operations tutorials (domains, SSH, monitoring)"
```

---

## Task 10: Write CLI Reference — Account & Project Commands

**Files:**
- Create: `docs/reference/cli/login.md`
- Create: `docs/reference/cli/new.md`
- Create: `docs/reference/cli/deploy.md`
- Create: `docs/reference/cli/dev.md`

Each page follows the CLI reference pattern: Synopsis, Description, Arguments & Flags (FlagTable), Examples (CommandBlock), Related Commands.

### login.md

Covers: `login`, `logout`, `me`

Flags for login: `--token <token>`
Flags for me: (none, inherits global flags)
Global flags section at bottom

Examples:
- `compozerr login` (interactive, opens browser)
- `compozerr login --token <token>` (CI/CD usage)
- `compozerr me` (view account info)
- `compozerr me --json` (JSON output for scripting)
- `compozerr logout`

### new.md

Covers: `new project`, `new module`

Flags for `new project`: `--location <iso>`, `--tier <tier>`, `--editor <name>`, `--no-editor`
Flags for `new module`: (name argument only)

Examples:
- `compozerr new project my-app`
- `compozerr new project my-app --location fra --tier T1`
- `compozerr new project my-app --editor cursor`
- `compozerr new module auth`

### deploy.md

Covers: `deploy`

Flags: `--wait`, `--poll-interval <ms>`

Examples:
- `compozerr deploy` (standard deploy)
- `compozerr deploy --wait` (wait for completion)
- `compozerr deploy --json` (JSON output for CI)

### dev.md

Covers: `dev`

No specific flags (inherits global).

Examples:
- `compozerr dev` (starts TUI with frontend + backend)
- Explain: frontend on port 3001, backend on port 5000
- Note: uses Deno scripts under the hood

**Step: Commit**

```bash
git add docs/reference/cli/login.md docs/reference/cli/new.md docs/reference/cli/deploy.md docs/reference/cli/dev.md
git commit -m "feat(docs): add CLI reference for account and project commands"
```

---

## Task 11: Write CLI Reference — Deployment & Operations Commands

**Files:**
- Create: `docs/reference/cli/deployment.md`
- Create: `docs/reference/cli/migrate.md`
- Create: `docs/reference/cli/ssh.md`

### deployment.md

Covers: `deployment status`, `deployment logs`, `deployment list`, `deployment latest`

Flags for `status`: `[deployment-id]` argument (optional)
Flags for `logs`: `[deployment-id]` argument (optional)
Flags for `list`: `--page <number>`, `--status <status>` (Queued, Deploying, Completed, Failed, Cancelled)
Flags for `latest`: (none)

Examples for each subcommand with expected output.

### migrate.md

Covers: `migrate`

Arguments: `[name]` (optional)
Flags: `--location <iso>`, `--tier <tier>`, `--skip-docker-setup`, `--redetect`

Examples:
- `compozerr migrate` (interactive detection)
- `compozerr migrate my-app --location fra`
- `compozerr migrate --redetect` (update template type for existing project)
- `compozerr migrate --skip-docker-setup` (skip Dockerfile wizard)

### ssh.md

Covers: `ssh`

Arguments: `[project-id]` (optional)
Flags: `--command <command>` / `-c <command>`

Examples:
- `compozerr ssh` (interactive terminal)
- `compozerr ssh -c "docker ps"` (one-off command)
- `compozerr ssh <project-id>` (specific project)
- `compozerr ssh -c "docker logs frontend" --json`

**Step: Commit**

```bash
git add docs/reference/cli/deployment.md docs/reference/cli/migrate.md docs/reference/cli/ssh.md
git commit -m "feat(docs): add CLI reference for deployment, migrate, and SSH commands"
```

---

## Task 12: Write CLI Reference — Module & Utility Commands

**Files:**
- Create: `docs/reference/cli/add-remove.md`
- Create: `docs/reference/cli/tiers.md`
- Create: `docs/reference/cli/cleanup.md`
- Create: `docs/reference/cli/update-reset.md`

### add-remove.md

Covers: `add <module>`, `remove <module>`

Examples:
- `compozerr add auth`
- `compozerr remove auth`

### tiers.md

Covers: `tiers`

Flags: `--location <iso>`

Examples:
- `compozerr tiers` (list all tiers)
- `compozerr tiers --location fra` (filter by location)
- `compozerr tiers --json`

Show example output table with RAM, cores, disk, price.

### cleanup.md

Covers: `cleanup`

No flags. Explain what gets cleaned: stopped containers, dangling images, builder cache.

Examples:
- `compozerr cleanup` with expected output showing space reclaimed

### update-reset.md

Covers: `update`, `reset`, and all global flags

Global flags section:
- `--json` - JSON output
- `-n, --non-interactive` - no prompts
- `-y, --yes` - auto-confirm
- `-q, --quiet` - suppress output
- `--force-interactive` - force interactive mode

Examples:
- `compozerr update`
- `compozerr reset`

**Step: Commit**

```bash
git add docs/reference/cli/add-remove.md docs/reference/cli/tiers.md docs/reference/cli/cleanup.md docs/reference/cli/update-reset.md
git commit -m "feat(docs): add CLI reference for module and utility commands"
```

---

## Task 13: Write Configuration Reference

**Files:**
- Create: `docs/reference/configuration/compozerr-json.md`
- Create: `docs/reference/configuration/environment-variables.md`

### compozerr-json.md

Frontmatter: `sidebar_position: 1`

Content:
1. **Overview** - what compozerr.json does, where it lives (project root)
2. **Project configuration** - full schema for `type: "project"`:
   - `type` (required): "project"
   - `name` (required): project name
   - `id`: auto-generated project ID (UUID)
   - `start`: custom start command (overrides default)
   - `cwd`: working directory for the start command
   - `templateType`: "docker-compose" | "dockerfile" | auto-detected
   - `templateFile`: path to Dockerfile or docker-compose.yml
3. **Module configuration** - full schema for `type: "module"`:
   - `type` (required): "module"
   - `name` (required): module name
   - `dependencies`: object of module dependencies
   - `start`: module start command
   - `port`: port the module runs on
   - `readyMessage`: string to watch for in output to know module is ready
   - `startupTimeoutMs`: timeout in ms
   - `frontend.routesDir`: path to frontend routes directory
   - `backend.defaultEnvProperties`: default environment variables
4. **Examples** for each project type pattern:
   - Compozerr template project (minimal)
   - Docker Compose project
   - Dockerfile project
   - Module with frontend routes
   - Module with backend env defaults

### environment-variables.md

Frontmatter: `sidebar_position: 2`

Content:
1. **How env vars work** - set in dashboard → pushed to VM on deploy → written to .env files
2. **Where env files are written** - backend/.env, frontend/.env, root .env
3. **Frontend vars** - `VITE_` prefix for Vite, `NEXT_PUBLIC_` for Next.js
4. **Build-time vs runtime** - explain docker build args vs runtime env
5. **Module defaults** - `backend.defaultEnvProperties` in compozerr.json
6. **Per-branch env vars** - explain branch-specific environment variables

**Step: Commit**

```bash
git add docs/reference/configuration/
git commit -m "feat(docs): add configuration reference (compozerr.json, env vars)"
```

---

## Task 14: Write Architecture Reference

**Files:**
- Create: `docs/reference/architecture/how-deployment-works.md`
- Create: `docs/reference/architecture/module-system.md`
- Create: `docs/reference/architecture/frontend-architecture.md`
- Create: `docs/reference/architecture/project-structure.md`

### how-deployment-works.md

Frontmatter: `sidebar_position: 1`

Conceptual pipeline (no internal implementation details):
1. You push code and run `compozerr deploy`
2. Deployment is queued (sequential processing)
3. A VM is provisioned or updated for your project
4. Your git repo is cloned/updated on the VM
5. Dependencies are installed
6. Environment variables are injected
7. Your start command runs (docker-compose, npm, etc.)
8. Domains are configured with Traefik
9. SSL certificates are auto-provisioned via Let's Encrypt
10. Deployment marked complete

Include a simple diagram (text-based or mermaid if Docusaurus supports it).

### module-system.md

Frontmatter: `sidebar_position: 2`

For full-stack template users:
1. **What is a module** - self-contained full-stack feature
2. **IFeature interface** - ConfigureBuilder, ConfigureServices, ConfigureApp lifecycle
3. **Backend patterns**:
   - MediatR commands/queries for business logic
   - Carter endpoints for HTTP routing
   - FluentValidation for request validation
4. **Frontend integration** - routesDir in compozerr.json, package.json exports
5. **Module directory structure** - annotated tree
6. **Example: building a simple module** - end-to-end walkthrough

### frontend-architecture.md

Frontmatter: `sidebar_position: 3`

For full-stack template users:
1. **TanStack Router** - file-based routing, naming conventions (`_layout.tsx`, `$param.tsx`, `index.tsx`)
2. **OpenAPI Qraft** - type-safe API client generation, how to regenerate
3. **Module route integration** - how modules contribute routes via compozerr.json
4. **API client usage** - `api.v1.*.useQuery()`, `api.v1.*.useMutation()` patterns

### project-structure.md

Frontmatter: `sidebar_position: 4`

Annotated directory tree of a Compozerr template project:
```
my-app/
├── backend/           # .NET 9.0 backend
│   ├── Api/           # Main API entry point
│   ├── Core/          # Shared infrastructure
│   └── Backend.sln    # Solution file
├── frontend/          # React + Vite frontend
│   ├── src/routes/    # File-based routing
│   ├── src/generated/ # Auto-generated API types
│   └── package.json
├── modules/           # Feature modules
├── scripts/           # Dev scripts (Deno)
├── compozerr.json     # Project configuration
├── docker-compose.yml # Container orchestration
├── Dockerfile.backend
├── Dockerfile.frontend
├── turbo.json         # Build orchestration
└── package.json       # Root workspace
```

With description of each key file/directory.

**Step: Commit**

```bash
git add docs/reference/architecture/
git commit -m "feat(docs): add architecture reference (deployment, modules, frontend, structure)"
```

---

## Task 15: Write Troubleshooting Pages

**Files:**
- Create: `docs/troubleshooting/project-wont-start.md`
- Create: `docs/troubleshooting/deployment-failures.md`
- Create: `docs/troubleshooting/domain-ssl-issues.md`
- Create: `docs/troubleshooting/common-errors.md`

### project-wont-start.md

Adapted from existing `why-your-project-wont-start.md` but expanded:
1. Check Docker is running
2. Check CLI version: `compozerr update`
3. Verify you're in the right directory (has compozerr.json)
4. Check port conflicts (5000, 3001)
5. Check .env file exists and has required vars
6. Try verbose logging
7. Clean Docker artifacts: `compozerr cleanup`

### deployment-failures.md

1. Check deployment logs: `compozerr deployment logs`
2. Common build failures: missing dependencies, wrong Node/dotnet version
3. Port conflicts in Docker
4. Missing environment variables
5. Docker build context issues
6. Start command failures (wrong command, missing scripts)
7. Timeout issues (deployment takes > 15 min)

### domain-ssl-issues.md

1. DNS propagation (can take up to 48h)
2. Wrong DNS record type (A vs CNAME)
3. SSL certificate not provisioning (DNS not pointing correctly)
4. Multiple domains on same project
5. Subdomain vs apex domain considerations

### common-errors.md

1. "Requires login" - run `compozerr login`
2. "Payment required" - add payment method in dashboard
3. "GitHub not connected" - reconnect GitHub in dashboard
4. "Uncommitted changes" - commit or stash before deploy
5. "Module not found" - check module name, verify it's published
6. "Tier unavailable" - check `compozerr tiers --location <iso>`

**Step: Commit**

```bash
git add docs/troubleshooting/
git commit -m "feat(docs): add troubleshooting pages"
```

---

## Task 16: Validation Pass — Review & Polish

Read through every page as if you're a new user. Check for:

1. **Broken links** - run `npm run build` (Docusaurus throws on broken links)
2. **Component imports** - every MDX file that uses custom components has correct imports
3. **Consistency** - all tutorials follow the same structure pattern
4. **CLI accuracy** - all command flags match actual CLI implementation
5. **Flow** - tutorials link logically via WhatNext components
6. **Readability** - clear headings, short paragraphs, good code examples
7. **Sidebar** - all pages appear in correct order in sidebar

**Step 1: Build the site**

```bash
cd modules/docs/frontend && npm run build
```

Fix any broken links or build errors.

**Step 2: Read every page**

Open http://localhost:1236 and click through every page. Check:
- Does the landing page make sense?
- Can you follow the quick start to completion?
- Do tutorials flow logically?
- Are CLI reference pages complete and accurate?
- Do "Read more" links go to the right places?
- Do "What's next" suggestions make sense?

**Step 3: Fix issues found**

Address any issues, then rebuild and re-check.

**Step 4: Final commit**

```bash
git add -A modules/docs/frontend
git commit -m "fix(docs): polish and fix issues from validation pass"
```

---

## Parallelization Notes

Tasks can be grouped for parallel execution:

- **Sequential**: Tasks 1-2 (infrastructure), then Task 3 (theme)
- **Parallel after Task 4**: Tasks 5-6 (landing + getting started) can run alongside Tasks 7-9 (tutorials) alongside Tasks 10-12 (CLI reference) alongside Tasks 13-14 (config + architecture reference) alongside Task 15 (troubleshooting)
- **Sequential**: Task 16 (validation) must run last

Recommended team structure for parallel execution:
- **Agent 1**: Tasks 5-6 (Getting Started content)
- **Agent 2**: Tasks 7-9 (Tutorial content)
- **Agent 3**: Tasks 10-12 (CLI Reference content)
- **Agent 4**: Tasks 13-15 (Configuration + Architecture + Troubleshooting content)
