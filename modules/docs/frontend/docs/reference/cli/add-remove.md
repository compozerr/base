---
sidebar_position: 8
title: add / remove
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# add / remove

Manage modules in your project.

## add

### Synopsis

```
compozerr add <name>
```

### Description

Add an existing module to the current project. The module is installed with its backend feature, frontend package, and any required dependencies. Module routes are automatically integrated into the application. Requires authentication and a clean Git working tree.

### Arguments

<FlagTable flags={[
  { name: "<name>", type: "string", description: "Name of the module to add." }
]} />

### Examples

Add the auth module:

<CommandBlock command="compozerr add auth" output="Adding module auth...
Installing backend dependencies...
Installing frontend package @repo/auth...
Updating compozerr.json...
Module auth added successfully!

Restart the dev server to load the new module." />

Add the stripe module:

<CommandBlock command="compozerr add stripe" output="Adding module stripe...
Installing backend dependencies...
Installing frontend package @repo/stripe...
Updating compozerr.json...
Module stripe added successfully!" />

Add a module non-interactively:

<CommandBlock command="compozerr add database --non-interactive" />

---

## remove

### Synopsis

```
compozerr remove <name>
```

### Description

Remove a module from the current project. This uninstalls the module's backend feature and frontend package, and updates the project configuration. Any database tables or data created by the module are not automatically removed.

### Arguments

<FlagTable flags={[
  { name: "<name>", type: "string", description: "Name of the module to remove." }
]} />

### Examples

Remove a module:

<CommandBlock command="compozerr remove billing" output="Removing module billing...
Removing backend project reference...
Removing frontend package @repo/billing...
Updating compozerr.json...
Module billing removed successfully!" />

Remove a module and auto-confirm:

<CommandBlock command="compozerr remove stripe --yes" output="Removing module stripe...
Removing backend project reference...
Removing frontend package @repo/stripe...
Updating compozerr.json...
Module stripe removed successfully!" />

---

## Related Commands

- [`new module`](/reference/cli/new) -- Create a new custom module
- [`deploy`](/reference/cli/deploy) -- Deploy after adding or removing modules
