```bash
fly deploy --app frontend-web-4b5e57 --config fly.frontend.toml --dockerfile Dockerfile.frontend
fly deploy --app backend-web-4b5e57 --config fly.backend.toml --dockerfile Dockerfile.backend
```
