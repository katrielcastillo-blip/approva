# Approva

Motor de aprobaciones empresariales configurable, multi-tenant.

**Estado:** funcional de punta a punta en local (Fases 0-4 completas, 5 y 6 en progreso).
Sin desplegar todavía — ver [Estado del despliegue](#estado-del-despliegue).

## El problema

Toda empresa tiene flujos de aprobación — compras, gastos, vacaciones, contrataciones — y
en casi todos los sistemas esos flujos están hardcodeados. Cambiar "las compras sobre
$5,000 necesitan al CFO" normalmente requiere que un programador toque código y despliegue.

## La solución

Las reglas viven en base de datos y las interpreta un motor (`WorkflowEngine`, en
[Approva.Domain](api/Approva.Domain/Services/WorkflowEngine.cs)). Un administrador define o
cambia el flujo desde la API (o desde el constructor de flujos del frontend) sin que nadie
recompile ni redespliegue nada. Ver la demostración en vivo abajo.

## Credenciales demo

Con la base sembrada (`docker compose up` + `dotnet run`, ver [Levantar en local](#levantar-en-local)),
el tenant `acme` (Acme Corp) ya tiene usuarios y ~9 solicitudes en distintos estados:

| Usuario | Email | Password | Rol |
|---|---|---|---|
| Admin | `admin@acme.test` | `Demo1234!` | Administrador (gestiona flujos y usuarios) |
| Manager | `ana.gomez@acme.test` | `Demo1234!` | Aprobador — paso "Manager" |
| CFO | `carlos.pena@acme.test` | `Demo1234!` | Aprobador — paso "CFO" (montos > $5,000) |
| CEO | `elena.ruiz@acme.test` | `Demo1234!` | Aprobador — paso "CEO" (montos > $50,000) |
| Solicitante | `luis.fernandez@acme.test` | `Demo1234!` | Reporta a Ana |
| Solicitante | `maria.torres@acme.test` | `Demo1234!` | Reporta a Ana |

**Recorrido recomendado:** entra como `luis.fernandez@acme.test`, crea una solicitud de
$6,500 (queda pendiente en el paso Manager) → sal y entra como `ana.gomez@acme.test`,
apruébala desde "Aprobaciones pendientes" → el motor la enruta automáticamente al paso CFO
(porque $6,500 > $5,000) sin que nadie haya tocado código.

## Estado del despliegue

Este repo está pensado para desplegarse como **frontend en Vercel + API en Render/Fly.io +
Postgres en Neon/Supabase** (Vercel no ejecuta contenedores .NET). Por ahora corre solo en
local — el despliegue implica crear cuentas en esos servicios, que no hice sin que el dueño
del proyecto lo confirme explícitamente. El `Dockerfile` y los workflows de CI ya están
listos para ese paso; ver [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#despliegue-objetivo-ver-readme-para-estado-actual).

## Decisiones técnicas

Detalle completo con diagramas en [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Resumen:

- **Las reglas son datos, no código** — `WorkflowDefinition`/`WorkflowStep`/`WorkflowCondition`
  en Postgres; el motor las interpreta en runtime.
- **Concurrencia optimista, no locks** — `ApprovalTask.RowVersion` mapeado al `xmin` de
  Postgres. Dos aprobadores decidiendo a la vez: uno gana (200), el otro pierde (409).
- **Auditoría append-only reforzada dos veces** — sin métodos de mutación en el dominio, y
  el `DbContext` rechaza explícitamente cualquier `Update`/`Delete` sobre `AuditEvent`.
- **Aislamiento multi-tenant por filtro global**, no por disciplina de cada query — probado
  con tests de integración reales (Testcontainers + Postgres) que crean dos tenants.
- **Qué haría diferente** — ver el final de [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#qué-haría-diferente).

## Estructura

```
api/             .NET 9, Clean Architecture
  Approva.Domain          — entidades, máquina de estados, motor de reglas (sin dependencias)
  Approva.Application     — CQRS (MediatR), validación (FluentValidation), puertos
  Approva.Infrastructure  — EF Core/Npgsql, JWT, BCrypt, Hangfire, seed de demo
  Approva.Api             — endpoints minimal API, auth, Swagger
  Approva.Tests           — xUnit: 38 unitarios (dominio) + 5 de integración (Testcontainers)
web/             Next.js 15 + TypeScript, TanStack Query, shadcn/ui (Radix), Recharts
infra/           docker-compose (Postgres)
docs/            ARCHITECTURE.md con diagramas
```

## Levantar en local

Requisitos: .NET 9 SDK, Node 20+, Docker.

1. Base de datos (Postgres vía Docker):

   ```bash
   cd infra
   docker compose up -d
   ```

2. API (.NET 9) — aplica migraciones y siembra el tenant demo en el primer arranque:

   ```bash
   cd api
   dotnet ef database update --project Approva.Infrastructure --startup-project Approva.Api
   dotnet run --project Approva.Api
   ```

   - Health check: `GET http://localhost:5080/health`
   - Swagger: `http://localhost:5080/swagger`
   - Dashboard de Hangfire (solo Development): `http://localhost:5080/jobs`

3. Web (Next.js):

   ```bash
   cd web
   cp .env.local.example .env.local
   npm install
   npm run dev
   ```

   Abre `http://localhost:3000` e inicia sesión con cualquiera de las [credenciales demo](#credenciales-demo).

## Correr los tests

```bash
cd api
dotnet test
```

43 tests: 38 unitarios de dominio (máquina de estados, motor de reglas — sin base de
datos) + 5 de integración contra un Postgres real levantado con Testcontainers, incluyendo
el test que el plan pedía explícitamente: **crear dos tenants y confirmar que uno no puede
leer los datos del otro** (`TenantIsolationTests`), y la carrera de dos aprobadores
decidiendo la misma tarea (`ConcurrencyTests`).

## Variables de entorno

| Variable | Dónde | Requerida | Nota |
|---|---|---|---|
| `ConnectionStrings:Default` | `api/Approva.Api/appsettings.json` | Sí | Ya apunta al Postgres de `docker-compose` |
| `Jwt:Secret` | `api/Approva.Api/appsettings.Development.json` | Sí | Valor de desarrollo ya incluido; **generar uno nuevo para producción** |
| `RESEND_API_KEY` | variable de entorno | No | Sin ella, las notificaciones de escalamiento por SLA se registran en el log en vez de enviarse por email (ver `LogNotificationSender`) |
| `NEXT_PUBLIC_API_URL` | `web/.env.local` | Sí | URL de la API; ya apunta a `localhost:5080` |

## Stack

- **Backend:** .NET 9 · EF Core 9 · PostgreSQL · MediatR · FluentValidation · Serilog ·
  Hangfire · JWT + BCrypt · xUnit + Testcontainers
- **Frontend:** Next.js 15 (App Router) · TypeScript · TanStack Query · Tailwind ·
  shadcn/ui · Recharts

## CI

GitHub Actions (`.github/workflows/ci.yml`) corre build + test del API (incluye los tests
de integración con Testcontainers) y lint + build del web en cada push y pull request a
`main`.

## Roadmap / qué queda deliberadamente fuera de esta versión

- Constructor de flujos drag & drop (el actual es basado en formularios — más rápido de
  construir, mismo resultado funcional)
- Aplicación móvil
- Notificaciones push / integración con Slack
- SSO empresarial (SAML, OAuth con Google)
- Internacionalización (todo el texto está en español)
- Despliegue a producción (Render + Vercel + Neon) — pendiente de confirmación explícita
