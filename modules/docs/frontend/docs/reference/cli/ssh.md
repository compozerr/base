---
sidebar_position: 7
title: ssh
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# ssh

Connect to a deployed project via SSH.

## Synopsis

```
compozerr ssh [project-id] [-c, --command <command>]
```

## Description

Open an interactive SSH session to a running deployment, or execute a one-off command remotely. When no project ID is provided, the CLI uses the project in the current directory. The interactive session gives you a shell inside the deployed container for debugging, inspecting logs, or running administrative tasks.

## Arguments

<FlagTable flags={[
  { name: "[project-id]", type: "string", description: "ID of the project to connect to. If omitted, uses the project in the current directory." }
]} />

## Flags

<FlagTable flags={[
  { name: "-c, --command", type: "string", description: "Execute a single command on the remote server instead of opening an interactive session. The connection closes after the command finishes." }
]} />

## Examples

Open an interactive SSH session:

<CommandBlock command="compozerr ssh" output="Connecting to my-saas (proj_a1b2c3)...
Connected.

root@my-saas:~#" />

Connect to a specific project:

<CommandBlock command="compozerr ssh proj_a1b2c3" output="Connecting to my-saas (proj_a1b2c3)...
Connected.

root@my-saas:~#" />

Run a one-off command to check disk usage:

<CommandBlock command='compozerr ssh -c "df -h"' output="Filesystem      Size  Used Avail Use% Mounted on
/dev/sda1        50G   12G   35G  26% /" />

Tail application logs remotely:

<CommandBlock command='compozerr ssh -c "tail -n 50 /var/log/app/stdout.log"' />

Run a database migration on the deployed project:

<CommandBlock command='compozerr ssh -c "cd /app && dotnet ef database update"' />

## Related Commands

- [`deploy`](/reference/cli/deploy) -- Deploy before connecting
- [`deployment logs`](/reference/cli/deployment) -- View deployment logs without SSH
- [`deployment status`](/reference/cli/deployment) -- Check deployment status
