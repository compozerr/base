---
sidebar_position: 3
title: Domain & SSL Issues
---

import CommandBlock from '@site/src/components/CommandBlock';

# Domain & SSL Issues

Custom domains require correct DNS configuration before SSL certificates can be provisioned.

## DNS not propagating

DNS changes can take up to 48 hours to propagate globally, though most complete within a few minutes. Check the current state of your DNS record:

<CommandBlock command="dig yourdomain.com +short" />

If this returns nothing or the wrong IP, your DNS change has not propagated yet. You can also use online tools like [dnschecker.org](https://dnschecker.org) to check propagation across regions.

## Wrong DNS record type

Use the correct record type for your domain:

| Domain type | Record | Example |
|-------------|--------|---------|
| Apex domain (`example.com`) | **A** record | Points to an IP address |
| Subdomain (`app.example.com`) | **CNAME** record | Points to your `.compozerr.app` subdomain |

A CNAME record on an apex domain will not work with most DNS providers. Use an A record instead.

## SSL certificate not provisioning

SSL certificates are provisioned automatically via Let's Encrypt once DNS is correctly configured. If your certificate is not being issued:

1. **Verify DNS** -- The domain must resolve to the correct server. Check with `dig`.
2. **Wait for propagation** -- Let's Encrypt needs to reach your server. If DNS just changed, wait a few minutes.
3. **Check the dashboard** -- The domain status in the dashboard shows whether DNS verification has passed.

Certificates cannot be issued if DNS does not point to your project's server.

## Certificate renewal

Certificates are renewed automatically before they expire. No action is required.

If auto-renewal fails, it is almost always because DNS records changed after the initial setup. Verify that your DNS records still point to the correct server.

## Multiple domains

You can assign multiple domains to a single project, or assign different domains to different services within the same project. Each domain must have its own DNS record pointing to your server.

Configure domain-to-service mappings in the dashboard under your project's **Settings > Domains**.
