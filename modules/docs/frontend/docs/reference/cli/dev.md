---
sidebar_position: 5
title: dev
---

import CommandBlock from '@site/src/components/CommandBlock';

# dev

Start the local development environment.

## Synopsis

```
compozerr dev
```

## Description

Launch the full development environment with hot-reload for both frontend and backend. By default, the command starts a Terminal User Interface (TUI) that displays logs from all services in an organized layout. If a TTY is not available (e.g., in a CI environment or piped output), it falls back to a non-interactive mode that streams logs directly to stdout.

The dev servers run on the following ports:

| Service  | Port |
|----------|------|
| Frontend | 3001 |
| Backend  | 5000 |

All file changes trigger automatic rebuilds. The frontend uses Vite with HMR for instant updates, and the backend uses `dotnet watch` for automatic recompilation.

## Examples

Start the development environment:

<CommandBlock command="compozerr dev" output="Starting development environment...

  Frontend: http://localhost:3001
  Backend:  http://localhost:5000

Press Ctrl+C to stop." />

Force non-interactive mode (useful for logging to a file):

<CommandBlock command="compozerr dev --non-interactive" output="[frontend] Vite dev server running at http://localhost:3001
[backend] Now listening on http://localhost:5000
[frontend] HMR connected
[backend] Application started" />

## TUI Mode

When running in a terminal, the TUI provides:

- Split view with frontend and backend logs
- Color-coded output by service
- Real-time error highlighting
- Keyboard shortcuts for navigation

If the TUI cannot be initialized (no TTY detected), logs stream directly to stdout with service prefixes.

## Related Commands

- [`new project`](/reference/cli/new) -- Create a project to develop
- [`deploy`](/reference/cli/deploy) -- Deploy when ready for production
