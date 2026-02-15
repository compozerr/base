---
sidebar_position: 7
title: SSH Into Your Project
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# SSH Into Your Project

Connect to your deployed project's VM to debug issues, inspect containers, or run one-off commands.

<Prerequisites items={["Compozerr CLI installed and authenticated", "A deployed Compozerr project"]} />

## 1. Open an interactive session

Start an SSH session into your project's VM:

<CommandBlock command="compozerr ssh" />

This drops you into a shell on the server where your project is running. You can explore files, inspect containers, and run commands directly.

## 2. Run a one-off command

If you just need to run a single command without an interactive session, use the `-c` flag:

<CommandBlock command='compozerr ssh -c "docker ps"' output="CONTAINER ID  IMAGE       STATUS         PORTS                  NAMES
a1b2c3d4e5f6  my-app_app  Up 2 hours     0.0.0.0:1234->3000/tcp my-app-app-1" />

The command runs on the server, prints the output, and exits.

## 3. Common debugging tasks

Once connected, here are the most useful commands:

**Check running containers:**

<CommandBlock command="docker ps" output="CONTAINER ID  IMAGE       STATUS         PORTS                  NAMES
a1b2c3d4e5f6  my-app_app  Up 2 hours     0.0.0.0:1234->3000/tcp my-app-app-1" />

**View container logs:**

<CommandBlock command="docker logs my-app-app-1 --tail 50" output="[INFO] Server started on port 3000
[INFO] Connected to database
[INFO] Ready to accept connections" />

Add `-f` to follow logs in real time: `docker logs my-app-app-1 -f`

**Restart services:**

<CommandBlock command="docker compose restart" output="Restarting my-app-app-1 ... done" />

**Check disk space:**

<CommandBlock command="df -h" output="Filesystem      Size  Used Avail Use% Mounted on
/dev/sda1        50G   12G   35G  26% /" />

**Inspect project files:**

<CommandBlock command="ls -la /main/" output="total 24
drwxr-xr-x 4 root root 4096 Feb 15 10:00 .
drwxr-xr-x 3 root root 4096 Feb 15 09:58 ..
-rw-r--r-- 1 root root  234 Feb 15 10:00 docker-compose.yml
-rw-r--r-- 1 root root  156 Feb 15 10:00 compozerr.json" />

## 4. SSH into a specific project

If you have multiple projects, specify which one to connect to:

<CommandBlock command="compozerr ssh my-project-id" />

<ReadMore to="/reference/cli/ssh" title="compozerr ssh" description="Full reference for the SSH command, including flags and connection options." />

<WhatNext items={[
  { title: "Monitor Deployments", to: "/tutorials/monitor-deployments", description: "Track deployment status, view logs, and debug failures." },
  { title: "Deploy a Docker Compose Project", to: "/tutorials/deploy-docker-compose", description: "Deploy any project with a docker-compose.yml to Compozerr." }
]} />
