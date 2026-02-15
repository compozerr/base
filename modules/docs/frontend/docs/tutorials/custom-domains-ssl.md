---
sidebar_position: 6
title: Set Up Custom Domains & SSL
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Set Up Custom Domains & SSL

Point your own domain to a deployed compozerr project. SSL certificates are provisioned automatically.

<Prerequisites items={["A deployed compozerr project", "A domain name you own", "Access to your domain's DNS settings"]} />

## 1. Add your domain in the dashboard

1. Go to [compozerr.com/projects](https://compozerr.com/projects)
2. Select your project
3. Navigate to **Settings > Domains**
4. Click **Add Domain** and enter your domain name (e.g., `example.com` or `app.example.com`)

## 2. Configure DNS records

Update your DNS settings at your domain registrar or DNS provider.

**For an apex domain** (e.g., `example.com`):

| Type | Name | Value |
|------|------|-------|
| A | @ | Your project's IP address |

**For a subdomain** (e.g., `app.example.com`):

| Type | Name | Value |
|------|------|-------|
| CNAME | app | your-project.compozerr.app |

Your project's IP address and hostname are shown in the dashboard when you add the domain.

## 3. Wait for DNS propagation

DNS changes can take up to 48 hours to propagate, though most providers update within minutes. You can check propagation status using:

<CommandBlock command="dig +short example.com" output="203.0.113.42" />

When the output shows your project's IP address, DNS is ready.

## 4. SSL is automatic

Once DNS points to your project, compozerr automatically provisions an SSL certificate via Let's Encrypt. No configuration is needed -- HTTPS just works.

Certificate renewal is also automatic. Certificates are renewed before they expire with zero downtime.

## 5. Verify

Visit your domain in a browser:

<CommandBlock command="curl -I https://example.com" output="HTTP/2 200
server: Traefik
strict-transport-security: max-age=31536000" />

You should see a valid HTTPS response.

## 6. Multiple domains

You can add multiple domains to a single project, or point different domains to different services. For example:

- `example.com` -- your frontend
- `api.example.com` -- your backend API

Add each domain separately in the dashboard and configure the corresponding DNS records.

## 7. Redeploy if needed

If you added domains after your initial deployment, redeploy to pick up the changes:

<CommandBlock command="compozerr deploy" />

<ReadMore to="/reference/architecture/how-deployment-works" title="How Deployment Works" description="Understand the full deployment pipeline including routing and SSL provisioning." />

<ReadMore to="/troubleshooting/domain-ssl-issues" title="Troubleshooting Domain & SSL Issues" description="Fix common DNS, domain, and SSL certificate problems." />

<WhatNext items={[
  { title: "SSH Into Your Project", to: "/tutorials/ssh-into-project", description: "Open a terminal into your deployed project for debugging and inspection." },
  { title: "Monitor Deployments", to: "/tutorials/monitor-deployments", description: "Track deployment status, view logs, and debug failures." }
]} />
