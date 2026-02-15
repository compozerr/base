---
sidebar_position: 9
title: tiers
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# tiers

List available server tiers and pricing.

## Synopsis

```
compozerr tiers [--location <iso>]
```

## Description

Display all available server tiers with their specifications and pricing. Tiers determine the CPU, memory, and storage allocated to your deployed project. Use `--location` to see tiers available in a specific region, as pricing and availability may vary by location.

## Flags

<FlagTable flags={[
  { name: "--location", type: "string", description: "Filter tiers by server location using an ISO country code (e.g., us, de, sg)." }
]} />

## Examples

List all available tiers:

<CommandBlock command="compozerr tiers" output="Tier        vCPU    RAM     Storage    Price/mo
starter     1       1 GB    25 GB      $5
pro         2       4 GB    80 GB      $20
business    4       8 GB    160 GB     $40
enterprise  8       32 GB   320 GB     $100" />

List tiers in a specific location:

<CommandBlock command="compozerr tiers --location de" output="Tier        vCPU    RAM     Storage    Price/mo    Location
starter     1       1 GB    25 GB      $5          Germany
pro         2       4 GB    80 GB      $21         Germany
business    4       8 GB    160 GB     $42         Germany" />

Get tiers as JSON:

<CommandBlock command="compozerr tiers --json" output='[{"name":"starter","vcpu":1,"ram":"1 GB","storage":"25 GB","price":5},{"name":"pro","vcpu":2,"ram":"4 GB","storage":"80 GB","price":20}]' />

## Related Commands

- [`new project`](/reference/cli/new) -- Create a project with a specific tier
- [`migrate`](/reference/cli/migrate) -- Migrate a project with a specific tier
- [`deploy`](/reference/cli/deploy) -- Deploy your project to production
