# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Backend for the Upkeep app, built with C# / .NET 8. REST API with JWT authentication, PostgreSQL via Supabase, and Entity Framework Core.

## Architecture

```
src/UpkeepAPI/
  Controllers/    → HTTP layer: routing, request/response, Swagger annotations
  DTOs/           → Input/output contracts (Auth/, User/, RoutineEvent/)
  Mappers/        → Static extension methods: Model → DTO
  Models/         → Domain entities (User + Habit/Routine graph), all inherit from BaseEntity
  Services/       → Business logic (Interfaces/ + implementations)
  Data/           → AppDbContext: EF Core config + auto timestamps
  Migrations/     → EF Core migrations
tests/UpkeepAPI.Tests/
  Fixtures/       → ApiFactory (WebApplicationFactory + Testcontainers Postgres)
  Integration/    → Endpoint tests (Health, Auth, Users, RoutineEvents, RefreshToken)
```

All code in English. User-facing messages (API responses, validation errors) in Portuguese.

## Commands

All commands run from the repo root.

```bash
dotnet restore                    # Restore packages
dotnet build                      # Build solution
dotnet run --project src/UpkeepAPI  # Run API
dotnet watch --project src/UpkeepAPI run  # Run with hot reload
dotnet test                       # Run all tests (requires Docker for Testcontainers)

dotnet ef migrations add <Name> --project src/UpkeepAPI  # Create migration
dotnet ef database update --project src/UpkeepAPI         # Apply migrations
```

## Key Conventions

- All models inherit `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`)
- `AppDbContext.SaveChangesAsync` sets timestamps automatically — never set them manually in services
- Credentials live in `.env` at the repo root only (never in `appsettings.json`); loaded via `Env.TraversePath().Load()` so the file is found regardless of CWD
- `.env` values use `__` to map to .NET config hierarchy (e.g. `Jwt__SecretKey` → `Jwt:SecretKey`)
- Quote `.env` values containing `$` with single quotes to prevent interpolation
- Keep code identifiers and source code in English
- Keep user-facing messages and validation output in Portuguese
- Rate limiting: global 100 req/min, `"auth"` policy 10 req/min (applied to auth + health routes); in `Testing` env both limits become `int.MaxValue`
- Offline-first: frontend will run a local DB and sync via the API. Keep `UpdatedAt` on every DTO and support `?updatedSince=<iso8601>` on list endpoints for delta sync (see `RoutineEventsController`). Server generates ids; conflict strategy is last-write-wins on `UpdatedAt`; deletes are hard (no tombstones yet).
- `RoutineEvent` supports two event types: **recurring** (`DaysOfWeek int[]`, repeats on given weekdays) and **once** (`EventDate DateOnly`, single occurrence). Exactly one must be set — validated in DTOs via `IValidatableObject` and in the service. `GET /routine-events` defaults to today; use `?from=&to=` for a date range. `?updatedSince=` bypasses the date filter (full delta sync).
- All timestamps are stored and returned in UTC. Clients are responsible for converting to the user's local timezone for display.
- Auth uses **access token (short-lived JWT) + refresh token (long-lived, 60 days default)**. Refresh tokens are persisted as SHA-256 hashes in `RefreshTokens` with `RevokedAt`. `POST /auth/refresh` rotates (revokes the old, issues a new pair) — never keep an old refresh token alive after refresh. `ClockSkew` is 5 min to tolerate device clock drift offline. `Jwt__RefreshExpirationInDays` env var controls refresh lifetime.
- Tests use Testcontainers with `postgres:16-alpine`; one container per test run, `TRUNCATE` between tests via `ApiFactory.ResetDatabaseAsync`

## Setup

1. Copy `.env.example` → `.env` at repo root and fill in credentials (Supabase connection string + JWT secret ≥ 32 chars)
2. `dotnet ef database update --project src/UpkeepAPI` — apply migrations
3. `dotnet run --project src/UpkeepAPI` — start API
4. `dotnet test` — run integration tests (requires Docker)

## Packages

| Package | Purpose |
|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL / Supabase |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth |
| `BCrypt.Net-Next` | Password hashing |
| `DotNetEnv` | Load `.env` via `Env.TraversePath().Load()` |
| `Swashbuckle.AspNetCore` + `.Annotations` | Swagger + `[SwaggerOperation]` |
| `Testcontainers.PostgreSql` | Postgres container for integration tests |
| `xunit` + `FluentAssertions` | Test framework + assertions |
