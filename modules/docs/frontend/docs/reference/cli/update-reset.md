---
sidebar_position: 11
title: update / reset / global flags
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# update / reset / global flags

Manage the CLI itself and configure output behavior.

## update

### Synopsis

```
compozerr update
```

### Description

Update the compozerr CLI to the latest version. The CLI checks for available updates and installs the newest release. After updating, the new version is available immediately.

### Examples

Update the CLI:

<CommandBlock command="compozerr update" output="Checking for updates...
Updating from v1.4.2 to v1.5.0...
Update complete! You are now on v1.5.0." />

When already on the latest version:

<CommandBlock command="compozerr update" output="Checking for updates...
You are already on the latest version (v1.5.0)." />

---

## reset

### Synopsis

```
compozerr reset
```

### Description

Reset the local CLI state. This clears cached data, stored credentials, and local configuration. After resetting, you will need to run `compozerr login` to authenticate again. Use this when troubleshooting CLI issues or switching between accounts.

### Examples

Reset all local state:

<CommandBlock command="compozerr reset" output="Resetting local CLI state...
Cleared cached data.
Cleared stored credentials.
Reset complete. Run 'compozerr login' to authenticate." />

Reset with auto-confirmation:

<CommandBlock command="compozerr reset --yes" output="Resetting local CLI state...
Cleared cached data.
Cleared stored credentials.
Reset complete." />

---

## Global Flags

The following flags are available on every compozerr CLI command.

<FlagTable flags={[
  { name: "--json", type: "boolean", description: "Output results as JSON. Useful for scripting and CI/CD pipelines." },
  { name: "-n, --non-interactive", type: "boolean", description: "Run without interactive prompts. The CLI will use defaults or fail if required input is missing." },
  { name: "-y, --yes", type: "boolean", description: "Auto-confirm all confirmation prompts. Skips 'Are you sure?' dialogs." },
  { name: "-q, --quiet", type: "boolean", description: "Suppress non-essential output. Only errors and critical information are displayed." },
  { name: "--force-interactive", type: "boolean", description: "Force interactive mode even when no TTY is detected. Useful in some terminal emulators." }
]} />

### Using Global Flags

Global flags can be placed before or after the command name.

Get JSON output from any command:

<CommandBlock command="compozerr me --json" output='{"email":"jane@example.com","displayName":"Jane Doe","uid":"usr_a1b2c3d4"}' />

Run a deployment non-interactively with auto-confirmation:

<CommandBlock command="compozerr deploy --non-interactive --yes --wait" />

Suppress output in scripts:

<CommandBlock command="compozerr deploy --quiet --wait" />

Combine flags for CI/CD pipelines:

<CommandBlock command="compozerr deploy --json --non-interactive --yes --wait" />

---

## Related Commands

- [`login`](/reference/cli/login) -- Authenticate after a reset
- [`me`](/reference/cli/login) -- Verify current account
- [`deploy`](/reference/cli/deploy) -- Deploy your project to production
