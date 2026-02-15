---
sidebar_position: 1
title: login / logout / me
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# login / logout / me

Manage your compozerr account authentication from the terminal.

## login

### Synopsis

```
compozerr login [--token <token>]
```

### Description

Authenticate with compozerr. In interactive mode, this opens your default browser for GitHub OAuth sign-in. For CI/CD pipelines and automated environments, pass a personal access token with the `--token` flag.

### Flags

<FlagTable flags={[
  { name: "--token", type: "string", description: "Authenticate with a personal access token instead of the browser flow. Required for CI/CD environments." }
]} />

### Examples

Sign in interactively via GitHub OAuth:

<CommandBlock command="compozerr login" output="Opening browser for GitHub authentication...
Successfully logged in as jane-doe" />

Sign in with a token for CI/CD:

<CommandBlock command="compozerr login --token cz_pat_abc123def456" output="Successfully authenticated." />

Verify you are logged in:

<CommandBlock command="compozerr me" output="Email: jane@example.com
Display Name: Jane Doe
User ID: usr_a1b2c3d4" />

---

## logout

### Synopsis

```
compozerr logout
```

### Description

Sign out of compozerr and clear stored credentials from your machine. After logging out, commands that require authentication will prompt you to sign in again.

### Examples

<CommandBlock command="compozerr logout" output="Logged out successfully." />

---

## me

### Synopsis

```
compozerr me [--json]
```

### Description

Display details about the currently authenticated account, including email, display name, and user ID. Useful for verifying which account is active or for scripting with `--json` output.

### Examples

View your account details:

<CommandBlock command="compozerr me" output="Email: jane@example.com
Display Name: Jane Doe
User ID: usr_a1b2c3d4" />

Get account info as JSON for scripting:

<CommandBlock command="compozerr me --json" output='{"email":"jane@example.com","displayName":"Jane Doe","uid":"usr_a1b2c3d4"}' />

---

## Related Commands

- [`new project`](/reference/cli/new) -- Create a new project after signing in
- [`deploy`](/reference/cli/deploy) -- Deploy your project
