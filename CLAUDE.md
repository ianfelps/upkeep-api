# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Backend for the Upkeep app, built with C# / .NET 8. REST API with JWT authentication, PostgreSQL via Supabase, and Entity Framework Core.

## Architecture

```
src/UpkeepAPI/
  Controllers/    → HTTP layer: routing, request/response, Swagger annotations
  DTOs/           → Input/output contracts (Auth/, User/, RoutineEvent/, Habit/, HabitLog/, UserProgress/)
  Mappers/        → Static extension methods: Model → DTO
  Models/         → Domain entities (User + Habit/Routine graph + UserProgress + Achievements), all inherit from BaseEntity
  Services/       → Business logic (Interfaces/ + implementations)
  Data/           → AppDbContext: EF Core config + auto timestamps
  Migrations/     → EF Core migrations
tests/UpkeepAPI.Tests/
  Fixtures/       → ApiFactory (WebApplicationFactory + Testcontainers Postgres)
  Integration/    → Endpoint tests (Health, Auth, Users, RoutineEvents, RefreshToken, Habits, HabitLogs, UserProgress, Achievements)
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
- `RoutineEvent` supports two event types: **recurring** (`DaysOfWeek int[]`, repeats on given weekdays) and **once** (`EventDate DateOnly`, single occurrence). Exactly one must be set — validated in DTOs via `IValidatableObject` and in the service. `GET /routine-events` defaults to today; use `?from=&to=` for a date range. `?updatedSince=` bypasses the date filter (full delta sync). `Color` (string?, max 7 chars, e.g. `#FF5733`) is optional display metadata; `IsActive` was removed.
- `Habit` has `Title` (max 100), `Description` (max 500), `LucideIcon` (max 50), `Color` (max 7, required), `FrequencyType` (enum: Daily=1, Weekly=2, Monthly=3), `TargetValue` (≥1), `IsActive` (bool). Linked to RoutineEvents via `HabitRoutineLink` join table. Create/Update DTOs accept optional `routineEventIds[]` — service replaces links atomically on update. `HabitDto` includes `linkedRoutineEventIds[]`. `GET /habits?updatedSince=` for delta sync.
- `HabitLog` tracks execution per day: `TargetDate` (DateOnly), `Status` (enum: Skipped=1, Missed=2, Completed=3), `CompletedAt` (set automatically when Status=Completed, cleared otherwise), `Notes` (max 500), `EarnedXP`. One log per (HabitId, TargetDate) — duplicates return 400. Nested under `/habits/{habitId}/logs`. Supports `?from=&to=` and `?updatedSince=`.
- `GET /habits/heatmap` returns `[{ date, completedCount, totalHabits }]` — only days with at least one log entry. Defaults to last 365 days. `totalHabits` = count of active habits at query time. Individual habit heatmap: use `GET /habits/{id}/logs?from=&to=`.
- All timestamps are stored and returned in UTC. Clients are responsible for converting to the user's local timezone for display.
- Auth uses **access token (short-lived JWT) + refresh token (long-lived, 60 days default)**. Refresh tokens are persisted as SHA-256 hashes in `RefreshTokens` with `RevokedAt`. `POST /auth/refresh` rotates (revokes the old, issues a new pair) — never keep an old refresh token alive after refresh. `ClockSkew` is 5 min to tolerate device clock drift offline. `Jwt__RefreshExpirationInDays` env var controls refresh lifetime.
- Achievements are predefined in code (`Models/AchievementDefinitions.cs`): `AchievementKey` enum + `AchievementDefinition` record + static `Achievements` class (list + `IsUnlocked` switch). `UserAchievement` model stores unlocked achievements (`UserId`, `Key` stored as string, unique index on `(UserId, Key)`, cascade delete). `IAchievementService.CheckAndUnlockAsync` is called at the end of `UserProgressService.GetProgressAsync` — it loads already-unlocked keys, computes which new ones qualify, and bulk-inserts only the new ones. `GET /users/me/achievements` returns all 14 achievements (locked + unlocked) with `isUnlocked` and `unlockedAt` (`CreatedAt` of the `UserAchievement` row). Adding new achievements requires only: adding a value to `AchievementKey`, an entry in `Achievements.All`, and a case in `Achievements.IsUnlocked` — no migration needed.
- `UserProgress` tracks gamification and stats per user: `CurrentLevel`, `TotalXP`, `CurrentStreak`, `LongestStreak`, `LastActivity`. One row per user (unique FK, cascade delete). Created with zeros on register via `IUserProgressService.SeedAsync`. `GET /users/me/progress` recomputes everything from `HabitLogs` and upserts the row — no manual update needed anywhere else. `CurrentLevel` = `floor(sqrt(TotalXP / 50)) + 1` (min 1). `CurrentStreak` is live only if the most recent completed date is today or yesterday. All stats not persisted in the model (`TotalHabitsActive`, `TotalLogsCompleted`, `CompletionRateLast7Days`, `CompletionRateLast30Days`) are computed on demand and returned in the DTO but not stored.
- Tests use Testcontainers with `postgres:16-alpine`; one container per test run, `TRUNCATE` between tests via `ApiFactory.ResetDatabaseAsync`. `ApiFactory.ConfigureWebHost` replaces `DbContextOptions<AppDbContext>` directly via `ConfigureServices` — do NOT use `Environment.SetEnvironmentVariable` for the connection string, as `Env.TraversePath().Load()` in `Program.cs` would overwrite it with the Supabase value.

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
