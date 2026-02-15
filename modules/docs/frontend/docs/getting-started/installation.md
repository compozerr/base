---
sidebar_position: 1
title: Installation
---

import CommandBlock from '@site/src/components/CommandBlock';
import ReadMore from '@site/src/components/ReadMore';

# Installation

## Install the CLI

Run the install script to download and configure the compozerr CLI:

<CommandBlock command='/bin/bash -c "$(curl -fsSL https://compozerr.com/install.sh)"' />

## Verify installation

Confirm the CLI is installed correctly:

<CommandBlock command="compozerr --version" output="compozerr/1.x.x" />

## Prerequisites

Before deploying, make sure you have:

- **Node.js >= 18** -- required for local development and the CLI
- **A GitHub account** -- used for authentication

## Sign in

Authenticate with your GitHub account. This opens your browser for OAuth:

<CommandBlock command="compozerr login" />

<ReadMore to="/reference/cli/login" title="compozerr login" description="Full reference for the login command, including flags and troubleshooting." />

## Payment setup

A payment method is required before you can deploy. Add one in the [compozerr dashboard](https://compozerr.com/projects) under **Settings > Billing**.
