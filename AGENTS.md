# Quizzy — Agent Guide

## Project Overview

A quiz platform with an ASP.NET Core 10 API backend and a planned Next.js 15 frontend (not yet created).

## Structure

```
Quizzy/
├── src/Quizzy.Api/          # ASP.NET Core 10 Web API (net10.0)
│   ├── Controllers/         # 9 controllers: Auth, Accounts, Categories, Quizzes, Questions, QuizAttempts, StudentAnswers, UserProfile, Roles
│   ├── Services/            # JwtTokenService, AccountDeletionService
│   ├── Data/                # ApplicationDbContext + 4 seeders (Role, User, Category, Quiz)
│   ├── Models/              # EF entities (ApplicationUser, ApplicationRole, UserProfile, Category, Quiz, Question, Choice, QuizAttempt, StudentAnswer)
│   └── DTOs/                # (not yet created)
├── tests/Quizzy.Api.Tests/  # xUnit tests (not yet implemented)
└── docs/                    # db_schema.md, api_endpoints.md
```

**Frontend (`src/Quizzy.Web/`)** is described in README but does not exist yet.

## Commands

| Task             | Command                                                         |
| ---------------- | --------------------------------------------------------------- |
| Restore          | `dotnet restore` (from repo root or `src/Quizzy.Api/`)          |
| Build            | `dotnet build`                                                  |
| Run API          | `dotnet run --project src/Quizzy.Api`                           |
| Run with seed    | `dotnet run --project src/Quizzy.Api --seed`                    |
| Add migration    | `dotnet ef migrations add <Name> --project src/Quizzy.Api`      |
| Apply migrations | `dotnet ef database update --project src/Quizzy.Api`            |
| Run tests        | `dotnet test`                                                   |
| Run single test  | `dotnet test --filter "FullyQualifiedName~<TestClassOrMethod>"` |

## Prerequisites

- .NET 10 SDK
- PostgreSQL 15+ running locally
- Connection string in `appsettings.Development.json` or user secrets (key: `ConnectionStrings:DefaultConnection`)

## Architecture Notes

- **Seeding runs on every startup** (`Program.cs:102-105`): RoleSeeder, UserSeeder, CategorySeeder, QuizSeeder execute before the app listens. Do not add expensive seed operations without guarding.
- **JWT SecretKey** is not in `appsettings.json`. Must be configured via user secrets or env var at `JwtSettings:SecretKey`.
- **Rate limiting** is active: login 5 req/min per IP, register 3 req/min per IP.
- **Soft deletes** on Category, Quiz, Question, Choice via `IsDeleted`/`DeletedAt`. Filter by `IsDeleted` in queries.
- **Questions are multiple-choice only** with exactly one correct answer (`Choices.IsCorrect`).
- **Snapshot pattern**: `StudentAnswers` stores frozen question/choice text so attempts remain valid after content edits.
- **Roles**: Teacher, Student, Admin (seeded at startup).

## Conventions

- Nullable reference types and implicit usings enabled.
- UUIDs for all entity primary keys except Identity (string).
- API returns standard error format: `{ statusCode, message, errors? }`.
- Swagger enabled in Development only.

## Docs

- `docs/db_schema.md` — full schema with ERD, constraints, cascade rules
- `docs/api_endpoints.md` — endpoint specs with request/response examples
