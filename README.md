# Compozerr

A modular monorepo combining a .NET 9.0 backend with a React + TypeScript frontend. Features a plugin-based module system where each module can contribute both backend APIs and frontend routes.

## Key Features

- **Modular Architecture**: Feature-based module system with auto-discovery
- **Type-Safe API**: OpenAPI generates TypeScript types from C# definitions
- **Modern Stack**: .NET 9 + React 19 + TanStack Router + TanStack Query
- **CQRS Pattern**: MediatR for all business logic with FluentValidation
- **File-Based Routing**: Frontend routes auto-generated from file structure

## Prerequisites

- Node.js >= 18
- .NET SDK 9.0
- Deno (for dev scripts)

## Quick Start

```bash
# Install all dependencies
npm run install:dep

# Start development servers (frontend + backend)
npm run dev
```

- Frontend: http://localhost:3001
- Backend: http://localhost:5000

## Project Structure

```
├── backend/           # .NET API
│   ├── Api/           # Main API entry point
│   └── Core/          # Shared infrastructure
├── frontend/          # React application
│   └── src/
│       ├── routes/    # File-based routing
│       └── generated/ # Auto-generated API client
└── modules/           # Feature modules
    ├── auth/          # Authentication
    ├── stripe/        # Payment processing
    └── ...
```

## Common Commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start frontend + backend |
| `npm run build` | Build everything |
| `cd frontend && npm run typecheck` | TypeScript check |
| `dotnet test` | Run backend tests |

## Deploy

```bash
compozerr deploy project
```

## Documentation

See [CLAUDE.md](./CLAUDE.md) for detailed architecture documentation.
