---
sidebar_position: 1
title: How Deployment Works
---

# How Deployment Works

When you run `compozerr deploy`, the following steps happen in sequence:

### 1. Push code

The CLI commits any staged changes, pushes your code to GitHub, and triggers a deployment.

### 2. Queue the deployment

Your deployment enters a sequential queue. Deployments run one at a time to prevent conflicts. If another deployment is in progress, yours waits until it completes.

### 3. Provision infrastructure

A VM is provisioned for your project. If your project already has a VM from a previous deployment, it is reused.

### 4. Clone or update the repository

Your Git repository is cloned on the VM. For subsequent deployments, the existing repository is updated with the latest changes.

### 5. Install dependencies

Dependencies are installed based on your project type -- `npm install`, `dotnet restore`, or whatever your stack requires.

**Note:** Bun is not supported as a runtime. Use Node.js and npm instead. See [Deployment Failures](/troubleshooting/deployment-failures#bun-runtime-not-supported) for details.

### 6. Inject environment variables

Environment variables configured in the dashboard are written to `.env` files in the appropriate locations on the VM.

### 7. Build and start

Your project is built and started using the command defined in `compozerr.json`. If no custom start command is set, the default is used.

For Docker Compose projects, this means running `docker compose up`. For template projects, the backend and frontend containers are built and started.

### 8. Configure routing

Domains assigned to your project are configured with the reverse proxy. Incoming HTTP and HTTPS traffic is routed to the correct service and port.

### 9. Provision SSL certificates

SSL certificates are automatically provisioned via Let's Encrypt for all configured domains. This happens after DNS records are verified.

### 10. Mark deployment complete

The deployment is marked as complete. Your application is live and accessible at its configured domains.

## Monitoring a deployment

Use `--wait` to watch the deployment in real time:

```
compozerr deploy --wait
```

Or check status after the fact:

```
compozerr deployment status <deployment-id>
```

View logs for a deployment:

```
compozerr deployment logs
```

## What can go wrong

If a deployment fails, it is rolled back automatically. The previous working version continues to serve traffic. Common failure reasons:

- **Build errors** -- Missing dependencies, syntax errors, or failing tests
- **Start command fails** -- The application crashes on startup
- **Missing environment variables** -- A required variable is not set in the dashboard
- **Timeout** -- The build or startup takes longer than 15 minutes

See [Deployment Failures](/troubleshooting/deployment-failures) for detailed troubleshooting.
