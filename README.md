# Approva

Motor de aprobaciones empresariales configurable, multi-tenant.

> En desarrollo — Fase 0 (Cimientos). Ver `docs/` para las decisiones de arquitectura a
> medida que se vayan tomando.

## Estructura

```
api/             .NET 9, Clean Architecture (Domain / Application / Infrastructure / Api / Tests)
web/             Next.js 15 + TypeScript
infra/           docker-compose y scripts
docs/            Diagramas, ADRs
```

## Levantar en local

1. Base de datos (Postgres vía Docker):

   ```bash
   cd infra
   docker compose up -d
   ```

2. API (.NET 9):

   ```bash
   cd api
   dotnet ef database update --project Approva.Infrastructure --startup-project Approva.Api
   dotnet run --project Approva.Api
   ```

   Health check: `GET http://localhost:5080/health`

3. Web (Next.js):

   ```bash
   cd web
   cp .env.local.example .env.local
   npm install
   npm run dev
   ```

## Stack

- **Backend:** .NET 9 · EF Core 9 · PostgreSQL · Serilog · xUnit
- **Frontend:** Next.js 15 (App Router) · TypeScript · Tailwind

## CI

GitHub Actions (`.github/workflows/ci.yml`) corre build + test del API y lint + build del
web en cada push y pull request a `main`.
