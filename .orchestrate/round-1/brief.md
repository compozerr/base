Multi-server support for Compozerr, plus admin-only server visibility.

Today Server entity exists with a ServerVisibility enum (Public, Private, Unknown). Server allocation lives on Project (Project.ServerId) and is auto-assigned by an event handler based on location + RAM load. ProjectEnvironment inherits the project's server. Domain routing uses per-server Traefik instances writing dynamic.yml; DNS resolves to the server's IP.

Wanted changes (one cohesive feature, but candidates for splitting into two specs A then B):

A) Server visibility + admin gating:
   1. Extend ServerVisibility enum with a third state (proposed name: AdminOnly — user originally said 'invite only' then suggested 'admin only'). Existing Public/Private semantics unchanged.
   2. Only platform admins can: (a) see AdminOnly servers in the server list, (b) be assigned to / create projects on an AdminOnly server. Need strong backend validation in the create-project / allocate-server path. Confirm whether 'admin' here means platform/system admin or org admin — the existing IOrganizationAuthorizationService.IsAdminAsync is org-scoped, which is probably the wrong gate for this. May need a platform-admin role/policy.
   3. Show the allocated server on the project / environment dashboard as a read-only property (display only, not editable — to change server the user must recreate the project/environment).

B) Per-environment server allocation + picker + domain routing:
   4. Move server allocation from Project to ProjectEnvironment (so different environments of the same project can live on different servers — e.g. prod in EU, preview in US). Schema migration + backfill from existing Project.ServerId to each ProjectEnvironment.ServerId.
   5. Server picker step in the create-project and create-environment flows (CLI + web). User picks at create time; after that it is immutable on the entity. AdminOnly servers visible in the picker only to platform admins.
   6. Domain / Traefik routing must follow the per-environment server: when an env is on server X, its subdomain / custom domains must route through that server's Traefik and DNS must point at that server's IP. Today primary domain is project-scoped — verify whether prod-vs-preview-on-different-servers requires per-environment DNS records / wildcard handling in the hosting repo (/Users/milo/milodev/gits/hosting). Out of scope: provisioning new bare-metal/VPS hosts (the Server CRUD endpoints already exist); focus on routing once a server is registered.

Out of scope: actually provisioning the underlying VPS in another country — assume operator already registers the Server row + sets up the host.

Key files surfaced from prior exploration:
- backend/Api.Data/Server.cs (ServerVisibility enum at lines 5-10)
- backend/Api.Data/Project.cs (ServerId FK at 31-32)
- backend/Api.Data/ProjectEnvironment.cs
- backend/Api.Data/Domain.cs
- modules/cli/backend/Cli/Endpoints/Projects/CreateProjectRoute.cs
- modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs
- backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentRoute.cs
- frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx (CreateEnvironmentDialog around line 530-857)
- modules/organizations/backend/Organizations/Services/IOrganizationAuthorizationService.cs (IsAdminAsync — org-scoped)
- /Users/milo/milodev/gits/hosting/manager/services/domain-service.ts (Traefik dynamic.yml writer)

User has indicated preference for shipping A first as its own PR, then B as a follow-up — but final decomposition is at the orchestrator's discretion after deep-thinking. Follow CLAUDE.md: regen the openapi-qraft client whenever endpoints change, use the generated api.v1.* client (no raw fetch), test in compozerr dev before claiming done.
