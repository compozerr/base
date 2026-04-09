# Service Logs Viewer

## Summary

Add a log viewer to the services section of the environment page. Each service row gets a small icon button that, when clicked, opens a slide-out sheet showing the last N lines of Docker logs for that service.

## Architecture

### Backend

**New endpoint**: `GET /projects/{projectId}/environments/{environmentId}/services/{serviceName}/logs?tail=200`

- Looks up the project's `ServerId` and the environment's `Name`
- Calls `IHostingApi.ExecuteCommandAsync(projectId, "docker compose logs --tail {tail} {serviceName}", environmentName)`
- Returns `{ logs: string, exitCode: int }` — raw log text and exit code

Uses the existing `ExecuteCommandAsync` path which goes through the hosting manager's `/projects/exec` endpoint, running the command via the Proxmox QEMU guest agent on the project's VM.

**Files**:
- `GetServiceLogsCommand.cs` — MediatR command with ProjectId, EnvironmentId, ServiceName, Tail
- `GetServiceLogsCommandHandler.cs` — resolves server, gets environment name, calls hosting API
- `GetServiceLogsRoute.cs` — maps `GET {environmentId}/services/{serviceName}/logs`
- Register in `EnvironmentsGroup.cs`

### Frontend

**UI**: Small `ScrollText` icon button in each service row's action area. Clicking opens a `Sheet` sliding in from the right containing:

- Header with service name
- Monospaced log output in a scrollable `<pre>` block with dark background
- Refresh button to re-fetch
- Tail line count selector (50 / 100 / 200 / 500)
- Loading state while fetching

**Data fetching**: Uses the generated API client's `useQuery` with `enabled: false` + manual `refetch()` so logs are only fetched on demand (when the sheet opens), not on page load.

## Scope

- No streaming/real-time tailing (would require WebSocket changes on hosting side)
- No log persistence or search
- Simple snapshot of recent logs via `docker compose logs --tail N`

## Error handling

- If the command fails (exit code != 0), show stderr in the sheet
- If the server is unreachable, show an error state
- If the service doesn't exist in Docker, the error from `docker compose logs` will naturally communicate that
