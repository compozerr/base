---
sidebar_position: 3
title: Deploy a Static Website
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Deploy a Static Website

Deploy any static site -- Vite, plain HTML, or any framework that outputs static files -- to compozerr using Nginx.

<Prerequisites items={["compozerr CLI installed and authenticated", "A static site project (Vite, plain HTML, etc.)", "Payment method configured in the dashboard"]} />

## 1. Add a Dockerfile

Create a multi-stage `Dockerfile` that builds your site and serves it with Nginx:

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

:::tip
If your build outputs to a directory other than `dist` (for example, `build/` for Create React App), update the `COPY` path accordingly.
:::

For a plain HTML site without a build step, skip the first stage:

```dockerfile
FROM nginx:alpine
COPY . /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## 2. Create an nginx.conf

Add an `nginx.conf` file in your project root. This configuration handles SPA routing by falling back to `index.html`:

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

If your site is not a single-page app and you want standard 404 behavior, replace the `try_files` line:

```nginx
    location / {
        try_files $uri $uri/ =404;
    }
```

## 3. Create a docker-compose.yml

Add a `docker-compose.yml` to your project root:

```yaml
services:
  app:
    build: .
    ports:
      - "1234:80"
```

## 4. Migrate to compozerr

Run the migrate command:

<CommandBlock command="compozerr migrate" output="Detected docker-compose.yml
? Project name: my-website
? Region: eu-central
Creating compozerr.json..." />

## 5. Deploy

Push your site to production:

<CommandBlock command="compozerr deploy" output="Deploying my-website...
Building services...
Build complete.
Your app is live at https://my-website.compozerr.app" />

Your static site is now live with automatic SSL.

<ReadMore to="/reference/cli/deploy" title="compozerr deploy" description="Full reference for the deploy command, including flags and deployment options." />

<WhatNext items={[
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your deployed site with automatic SSL." },
  { title: "Monitor Deployments", to: "/tutorials/monitor-deployments", description: "Track deployment status, view logs, and debug failures." }
]} />
