# COM5043 Warehouse Management System

## Project Status

This repository is currently in the design and setup phase.

The focus so far has been:
- Defining scope and assumptions
- Setting up a clean multi-project architecture
- Setting up CI and quality tooling
- Preparing frontend and backend foundations for implementation

## What Has Been Completed

### 1) Architecture and Solution Setup

Backend solution scaffolded as a layered/clean architecture in `WMS-API`:
- `Wms.Api`: API host and configuration
- `Wms.Application`: use-case/application layer
- `Wms.Domain`: core business/domain layer
- `Wms.Infrastructure`: infrastructure concerns
- `Wms.Contracts`: DTO and contract boundary
- `Wms.Common`: shared cross-cutting code

Supporting setup:
- `.NET SDK` pinned in `WMS-API/global.json`
- Solution file and project references configured in `WMS-API/Wms.sln`
- Docker build/run setup in `WMS-API/docker/Dockerfile.api` and `WMS-API/docker/docker-compose.yml`

### 2) Design Documentation

Design documentation has started under `docs/design`:
- `docs/design/00-scope-and-assumptions.md`
- `docs/design/01-requirements.md`
- `docs/design/02-architecture.md`

API documentation currently available:
- `docs/api/00-api-coding-standards.md`

Scaffolded files prepared for next API design outputs:
- `docs/api/endpoints.md`
- `docs/api/dto-definitions.md`

### 3) Frontend Foundation

Angular frontend scaffolded in `WMS-Frontend` with:
- Angular 21 + TypeScript
- strict linting via ESLint
- formatting via Prettier
- unit test setup via Jasmine + Karma
- CI test command with coverage (`npm run test:ci`)

### 4) CI, Quality Gates, and Tooling

GitHub Actions workflows are configured:
- `.github/workflows/backend.yml`: restore/build/test .NET
- `.github/workflows/frontend.yml`: format/lint/test/build Angular
- `.github/workflows/sonar.yml`: SonarCloud analysis for backend and frontend

Sonar configuration is set in `WMS-API/sonar-project.properties` and aligned to this repo's actual folder structure.

## Repository Structure

```text
COM5043_Warehouse-Management-system/
├─ .github/workflows/
│  ├─ backend.yml
│  ├─ frontend.yml
│  └─ sonar.yml
├─ docs/
│  ├─ design/
│  │  ├─ 00-scope-and-assumptions.md
│  │  ├─ 01-requirements.md
│  │  └─ 02-architecture.md
│  └─ api/
│     ├─ 00-api-coding-standards.md
│     ├─ endpoints.md
│     └─ dto-definitions.md
├─ WMS-API/
│  ├─ src/
│  │  ├─ Wms.Api/
│  │  ├─ Wms.Application/
│  │  ├─ Wms.Domain/
│  │  ├─ Wms.Infrastructure/
│  │  ├─ Wms.Contracts/
│  │  └─ Wms.Common/
│  ├─ tests/
│  ├─ docker/
│  ├─ Wms.sln
│  ├─ global.json
│  └─ sonar-project.properties
└─ WMS-Frontend/
   ├─ src/
   ├─ angular.json
   ├─ eslint.config.js
   └─ package.json
```

## Tech Stack

- Backend: C# / .NET 8 / ASP.NET Core Web API / xUnit
- Frontend: Angular 21 / TypeScript / Jasmine + Karma / ESLint / Prettier / SCSS
- Quality & DevOps: GitHub Actions / SonarCloud / Docker
- Database: MySQL 8 + Entity Framework Core (Pomelo provider)

## Database Setup (MySQL + EF Core)

The backend is now configured for:
- MySQL provider via `Pomelo.EntityFrameworkCore.MySql`
- EF Core DbContext in `WMS-API/src/Wms.Infrastructure/Persistence/WmsDbContext.cs`
- entity mapping in `WMS-API/src/Wms.Infrastructure/Persistence/Configurations/`
- design-time factory for migrations in `WMS-API/src/Wms.Infrastructure/Persistence/WmsDbContextFactory.cs`

Connection string key:
- `ConnectionStrings:DefaultConnection` in `WMS-API/src/Wms.Api/appsettings.json`

Docker compose includes:
- `wms-mysql` service (MySQL 8.4)
- `wms-api` service configured to connect to the MySQL container

Start both services:

```bash
docker compose -f WMS-API/docker/docker-compose.yml up --build
```

### EF Core Migration Commands

From repository root:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project WMS-API/src/Wms.Infrastructure --startup-project WMS-API/src/Wms.Api --output-dir Persistence/Migrations
dotnet ef database update --project WMS-API/src/Wms.Infrastructure --startup-project WMS-API/src/Wms.Api
```

## Current Implementation Note

This is intentionally a setup-first stage:
- The codebase currently contains scaffolding and baseline templates.
- Business entities, use cases, endpoints, and detailed tests are the next phase.
- The project structure is ready for that implementation work.
