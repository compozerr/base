---
sidebar_position: 2
title: Deploy a Next.js App
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Deploy a Next.js App

Deploy an existing Next.js application to Compozerr with Docker and automatic SSL.

<Prerequisites items={["Compozerr CLI installed and authenticated", "An existing Next.js project", "Payment method configured in the dashboard"]} />

## 1. Add a Dockerfile

Compozerr deploys Docker containers, so your Next.js app needs a Dockerfile. Create one in your project root using a multi-stage build for optimal image size:

```dockerfile
FROM node:20-alpine AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci

FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY --from=builder /app/public ./public
EXPOSE 3000
CMD ["node", "server.js"]
```

:::tip
This Dockerfile uses Next.js [standalone output](https://nextjs.org/docs/app/api-reference/config/next-config-js/output). Make sure your `next.config.js` includes `output: 'standalone'`.
:::

## 2. Create a docker-compose.yml

If you don't already have one, add a `docker-compose.yml` to your project root:

```yaml
services:
  app:
    build: .
    ports:
      - "1234:3000"
```

The left side of the port mapping (`1234`) is the external port Compozerr uses for routing. The right side (`3000`) must match the `EXPOSE` in your Dockerfile.

## 3. Handle NEXT_PUBLIC_ environment variables

`NEXT_PUBLIC_` variables are inlined at **build time**, not runtime. Pass them as build args in your `docker-compose.yml`:

```yaml
services:
  app:
    build:
      context: .
      args:
        - NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}
    ports:
      - "1234:3000"
```

Then reference them in your Dockerfile:

```dockerfile
FROM node:20-alpine AS builder
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
# ... rest of build stage
```

Set the actual values in the Compozerr dashboard under **Settings > Environment Variables**.

<ReadMore to="/reference/configuration/environment-variables" title="Environment Variables" description="How environment variables work during build and runtime." />

## 4. Migrate to Compozerr

Run the migrate command from your project directory:

<CommandBlock command="compozerr migrate" output="Detected docker-compose.yml
? Project name: my-nextjs-app
? Region: eu-central
Creating compozerr.json..." />

<ReadMore to="/reference/cli/migrate" title="compozerr migrate" description="Full reference for the migrate command, including flags and non-interactive mode." />

## 5. Deploy

Push to production:

<CommandBlock command="compozerr deploy" output="Deploying my-nextjs-app...
Building services...
Build complete.
Your app is live at https://my-nextjs-app.compozerr.app" />

Your Next.js app is now live with automatic SSL.

<WhatNext items={[
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your deployed project with automatic SSL." },
  { title: "SSH Into Your Project", to: "/tutorials/ssh-into-project", description: "Open a terminal into your deployed project for debugging." }
]} />
