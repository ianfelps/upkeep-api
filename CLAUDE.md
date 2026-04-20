# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Backend for the Upkeep app, built with C# / .NET 8. REST API with JWT authentication, PostgreSQL via Supabase, and Entity Framework Core.

## Architecture

```
Controllers/    → HTTP layer: routing, request/response, Swagger annotations
DTOs/           → Input/output contracts (Auth/, User/)
Mappers/        → Static extension methods: Model → DTO
Models/         → Domain entities, all inherit from BaseEntity
Services/       → Business logic (Interfaces/ + implementations)
Data/           → AppDbContext: EF Core config + auto timestamps
```

All code in English. User-facing messages (API responses, validation errors) in Portuguese.

## Commands

```bash
dotnet build UpkeepAPI.csproj     # Build
dotnet run --project UpkeepAPI.csproj  # Run API
dotnet watch run                  # Run with hot reload

dotnet ef migrations add <Name>   # Create migration
dotnet ef database update         # Apply migrations
```

## Key Conventions

- All models inherit `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`)
- `AppDbContext.SaveChangesAsync` sets timestamps automatically — never set them manually in services
- Credentials live in `.env` only (never in `appsettings.json`)
- `.env` values use `__` to map to .NET config hierarchy (e.g. `Jwt__SecretKey` → `Jwt:SecretKey`)
- Quote `.env` values containing `$` with single quotes to prevent interpolation
- Rate limiting: global 100 req/min, `"auth"` policy 10 req/min (applied to auth + health routes)

## Packages

| Package | Purpose |
|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL / Supabase |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth |
| `BCrypt.Net-Next` | Password hashing |
| `DotNetEnv` | Load `.env` file |
| `Swashbuckle.AspNetCore` + `.Annotations` | Swagger + `[SwaggerOperation]` |
