---
sidebar_position: 10
title: cleanup
---

import CommandBlock from '@site/src/components/CommandBlock';

# cleanup

Clean Docker build artifacts from your local machine.

## Synopsis

```
compozerr cleanup
```

## Description

Remove Docker artifacts created by Compozerr development and build processes. This reclaims disk space by cleaning up stopped containers, dangling images, and builder cache. This is a local operation and does not affect your deployed projects.

The cleanup targets:

- **Stopped containers** -- Containers that are no longer running from previous dev sessions
- **Dangling images** -- Intermediate build images that are no longer referenced
- **Builder cache** -- Docker BuildKit cache used during image builds

## Examples

Run cleanup:

<CommandBlock command="compozerr cleanup" output="Cleaning up Docker artifacts...
Removed 3 stopped containers
Removed 12 dangling images
Cleared 2.4 GB of builder cache
Done. Reclaimed 3.1 GB of disk space." />

Run cleanup with auto-confirmation:

<CommandBlock command="compozerr cleanup --yes" output="Cleaning up Docker artifacts...
Removed 1 stopped container
Removed 5 dangling images
Cleared 800 MB of builder cache
Done. Reclaimed 1.2 GB of disk space." />

Run cleanup quietly:

<CommandBlock command="compozerr cleanup --quiet" />

## Related Commands

- [`dev`](/reference/cli/dev) -- Development environment that creates Docker artifacts
- [`deploy`](/reference/cli/deploy) -- Build and deploy process that creates images
