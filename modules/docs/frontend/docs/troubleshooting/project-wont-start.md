---
sidebar_position: 1
title: Project Won't Start
---

import CommandBlock from '@site/src/components/CommandBlock';

# Project Won't Start

Common reasons your project fails to start locally, and how to fix them.

## Docker not running

The development environment requires Docker. Make sure Docker Desktop is running:

<CommandBlock command="docker info" />

If this returns an error, open Docker Desktop and wait for it to finish starting.

## CLI outdated

An outdated CLI can cause compatibility issues. Update to the latest version:

<CommandBlock command="compozerr update" />

## Wrong directory

The CLI must be run from a directory that contains a `compozerr.json` file. If you see a "project not found" error, check your working directory:

<CommandBlock command="ls compozerr.json" />

If the file does not exist, navigate to your project root.

## Port conflicts

The compozerr template uses port **1234** for the frontend and **1235** for the backend. If another process is using these ports, startup will fail.

Check for conflicts:

<CommandBlock command="lsof -i :1234" />
<CommandBlock command="lsof -i :1235" />

Kill the conflicting process or configure your project to use different ports.

## Missing .env file

The backend requires environment variables to start. Check that `backend/.env` exists and contains the required values:

<CommandBlock command="ls backend/.env" />

If it is missing, copy the example and fill in the values:

<CommandBlock command="cp backend/.env.example backend/.env" />

## Node.js version

compozerr requires Node.js 18 or later:

<CommandBlock command="node --version" />

If your version is older, update Node.js using your preferred version manager (nvm, fnm, etc.).

## .NET SDK version

The compozerr template requires .NET SDK 9.0:

<CommandBlock command="dotnet --version" />

If you have a different version, install .NET 9.0 from [dot.net](https://dot.net/download).

## Clean Docker state

Stale containers or images from previous runs can cause startup failures. Clean them up:

<CommandBlock command="compozerr cleanup" />

This removes old containers, images, and volumes associated with your project. It does not affect other Docker workloads.
