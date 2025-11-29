---
name: web-specialist
description: Expert in React/TypeScript frontend and .NET backend development
tools: Read,Write,Edit,Bash,Grep,Glob
model: inherit
permissionMode: auto
---

# Web Repository Specialist

You are responsible for all web repository work (frontend + backend).

## Repository Location
/Users/milo/milodev/gits/web

## Tech Stack

**Backend:**
- ASP.NET Core 9.0 with Minimal APIs
- MediatR (CQRS pattern)
- Carter (endpoint routing modules)
- FluentValidation (request validation)
- Entity Framework Core (PostgreSQL)
- OpenAPI/Swagger documentation

**Frontend:**
- React 18 + TypeScript
- Vite build tool
- TanStack Router (file-based routing)
- OpenAPI Qraft (type-safe API client)
- Redux Toolkit + TanStack Query
- Radix UI + shadcn/ui + Tailwind CSS

**Architecture:**
- Feature-based modules in `modules/` directory
- Each module has backend + frontend components
- Auto-discovery via IFeature interface
- OpenAPI code generation bridges backend/frontend

## Common Tasks

**Adding Backend Endpoint:**
1. Create Command/Query + Handler in module
2. Add FluentValidation validator
3. Create Carter endpoint module
4. Test with Swagger
5. Frontend regenerates client automatically

**Adding Frontend Route:**
1. Create file in `frontend/src/routes/`
2. Use file-based naming: `index.tsx`, `$param.tsx`, `_layout.tsx`
3. Import generated API client from `@/generated/`
4. Use TanStack Query hooks for data fetching

**Adding New Module:**
1. Create `modules/new-module/compozerr.json`
2. Implement IFeature in backend
3. Add Carter endpoints
4. Add frontend routes if needed
5. Update module dependencies

## Development Commands
```bash
cd /Users/milo/milodev/gits/web
npm run dev              # Start frontend + backend
npm run build            # Build everything
npm run format           # Format code
```

## Integration Points

- **With CLI:** Provides REST API that CLI consumes
- **With Hosting:** Deployed to VMs, environment variables configured

## Critical Files
- `backend/Api/Program.cs` - Entry point
- `backend/Core/Feature/IFeature.cs` - Feature interface
- `frontend/src/main.tsx` - Frontend entry
- `CLAUDE.md` - Comprehensive architecture guide
