# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Backend for the Upkeep app, built with C# / .NET 8. REST API with JWT authentication, PostgreSQL via Supabase, and Entity Framework Core.

## Architecture

```
src/UpkeepAPI/
  Controllers/    → HTTP layer: routing, request/response, Swagger annotations
  DTOs/           → Input/output contracts (Auth/, User/)
  Mappers/        → Static extension methods: Model → DTO
  Models/         → Domain entities (User + Habit/Routine graph), all inherit from BaseEntity
  Services/       → Business logic (Interfaces/ + implementations)
  Data/           → AppDbContext: EF Core config + auto timestamps
  Migrations/     → EF Core migrations
tests/UpkeepAPI.Tests/
  Fixtures/       → ApiFactory (WebApplicationFactory + Testcontainers Postgres)
  Integration/    → Endpoint tests (Health, Auth, Users)
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
