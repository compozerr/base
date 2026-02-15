---
sidebar_position: 4
title: Common Errors
---

import CommandBlock from '@site/src/components/CommandBlock';

# Common Errors

Quick reference for error messages you may encounter and how to resolve them.

## "Requires login"

You are not authenticated. Sign in with your GitHub account:

<CommandBlock command="compozerr login" />

## "Payment required"

A payment method is required before deploying. Add one in the [compozerr dashboard](https://compozerr.com/projects) under **Settings > Billing**.

## "GitHub not connected"

Your GitHub account is not linked or the connection expired. Reconnect in the dashboard under **Settings > GitHub**, or re-run:

<CommandBlock command="compozerr login" />

## "Uncommitted changes"

The CLI requires a clean Git working tree before deploying. Commit or stash your changes:

<CommandBlock command="git add . && git commit -m 'prepare for deploy'" />

Or stash them temporarily:

<CommandBlock command="git stash" />

## "Module not found"

The specified module does not exist or is not available. Check the module name is correct and that it exists in your project's `modules/` directory.

List the modules in your project:

<CommandBlock command="ls modules/" />

## "Tier unavailable in location"

The server tier you selected is not available in the chosen location. Check available tiers for your location:

<CommandBlock command="compozerr tiers --location <iso>" />

Replace `<iso>` with your location's ISO code (e.g., `us`, `eu`, `de`).

## "Project not found"

The CLI cannot find a valid project in the current directory. Make sure:

1. You are in a directory containing a `compozerr.json` file
2. The `compozerr.json` has a valid `id` field
3. The project exists on your account (it may have been deleted from the dashboard)

Check your current configuration:

<CommandBlock command="cat compozerr.json" />
