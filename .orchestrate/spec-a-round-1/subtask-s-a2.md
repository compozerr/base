# Subtask s-a2 — Frontend: regen API client + show allocated server on dashboards

**Intent:** Regenerate the openapi-qraft client (picks up `GET /v1/servers` from S-A1) and surface the allocated server on the project page and environment rows as a read-only field. No picker, no edit. Test in the browser via `compozerr dev` before claiming done.

**Suggested files:**
- `frontend/src/generated/` — regenerated wholesale (do not hand-edit)
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx` (project page — show `Server: <name> (<location>)`)
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx` (env rows — show server per env)

**Acceptance criteria:**
- [ ] `cd frontend && npm run typecheck` passes
- [ ] `cd frontend && npm run build` succeeds
- [ ] Generated client exposes the new endpoint hook (e.g. `api.v1.getServers.useQuery()`)
- [ ] All API calls use the generated `api.v1.*` client; NO raw `fetch`, `useQuery`, `useMutation`, `orgFetch`
- [ ] Project detail page shows the allocated server's name (and location if available) as a read-only field; renders a graceful "Not yet allocated" placeholder when `project.serverId` is null
- [ ] Each environment row shows its allocated server, sourced from the data shape (do not hardcode `project.server` — pull from whatever the API returns at env-level even if it's the same as project for Spec A)
- [ ] Visually verified in the browser via `compozerr dev` — both surfaces render a server name. **Verification MUST be done via the `puppeteer` MCP** (`mcp__puppeteer__puppeteer_navigate`, `mcp__puppeteer__puppeteer_screenshot`) against `http://localhost:1234`. Log in (test creds from `backend/.env` or DevSeeder), navigate to a project's detail page → screenshot showing "Server" field, then to the environments page → screenshot showing per-env server. **Both screenshots must be captured and saved to `.orchestrate/round-1/verification/` in the run worktree, named `s-a2-project-page.png` and `s-a2-environments-page.png`.** If puppeteer cannot reach the app (login flow blocks it, OAuth required, etc.), STOP and report in the final session message: `VERIFICATION_BLOCKED: <reason>` and tell the user to manually verify — do NOT silently skip.
- [ ] All commits on branch `task/s-a2`; commit the regenerated `frontend/src/generated/` diff as ITS OWN commit before the consumer-code commit, so reviewers can diff in isolation

**Notes / gotchas:**
- This subtask depends on S-A1 being merged. Do not start before the orchestrator signals it.
- Regenerate command (CLAUDE.md, port 1235 for `compozerr dev`):
  ```
  cd frontend && npx openapi-qraft \
    --plugin tanstack-query-react --plugin openapi-typescript \
    http://localhost:1235/swagger/v1/swagger.json \
    --clean -o src/generated --export-type \
    --properties-required-by-default --default-non-nullable
  ```
  Start `compozerr dev` first and wait for swagger to respond.
- If the regen diff includes endpoints unrelated to S-A1, a backend route signature drifted somewhere else. STOP, report as STUCK with the unrelated diff snippet — do NOT bury it in this feature PR.
- Do NOT add a server picker or any edit affordance — that's Spec B. Read-only display only.
- The full plan + scope decision is at `.orchestrate/round-1/plan.md`. Skim it before starting so you understand the boundary.
- **Puppeteer verification is mandatory.** The user explicitly asked for it. The puppeteer MCP tools are deferred and must be loaded via `ToolSearch` with `query: "select:mcp__puppeteer__puppeteer_navigate,mcp__puppeteer__puppeteer_screenshot,mcp__puppeteer__puppeteer_click,mcp__puppeteer__puppeteer_fill"`. Start `compozerr dev` first; verify swagger responds; verify frontend at `http://localhost:1234` responds; then drive the flow. If login is the blocker, look in `backend/Api/DevSeeder.cs` for seeded test credentials.
