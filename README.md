# Warehouse Management System

Coursework project with a .NET 8 API, an Angular frontend, and a MySQL database.

## Requirements

Install these before running the project:

- `Git` to clone/download the repository
- `.NET SDK 8` for the API and backend tests
- `Node.js 20 LTS` with `npm 10+` for the Angular frontend
- `Docker Desktop` recommended for the database

Optional:

- `MySQL 8` if you want to run the database without Docker
- `Google Chrome` or `Chromium` if you want to run the frontend test suite locally

Recommended version check commands:

```bash
dotnet --version
node --version
npm --version
docker --version
```

## Structure

- `WMS-API` - ASP.NET Core API, application logic, domain model, infrastructure, tests
- `WMS-Frontend` - Angular frontend
- `docs` - design notes and API documentation

## Quick start

The simplest setup is:

1. Run the MySQL database with Docker
2. Run the API locally
3. Run the frontend locally

Default local URLs:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5021`
- Swagger: `http://localhost:5021/swagger`

## Run with Docker

From the repository root:

```bash
docker compose -f WMS-API/docker/docker-compose.yml up -d wms-mysql
docker compose -f WMS-API/docker/docker-compose.yml up --build wms-api
```

- API: `http://localhost:5021`
- Swagger: `http://localhost:5021/swagger`

The API now applies pending EF Core migrations automatically on startup, which helps local workflow changes stay in sync with the database schema.

## Run locally

Start MySQL first:

```bash
docker compose -f WMS-API/docker/docker-compose.yml up -d wms-mysql
```

Run the API:

```bash
cd WMS-API
dotnet restore
dotnet run --project src/Wms.Api
```

Run the frontend:

```bash
cd WMS-Frontend
npm install
npm start
```

The frontend uses a proxy and expects the API on `http://localhost:5021`.

### Database details

If you use the provided Docker setup, MySQL runs with:

- Host: `localhost`
- Port: `3306`
- Database: `wms_db`
- Username: `wms_user`
- Password: `wms_pass`

## Checks

Backend:

```bash
cd WMS-API
dotnet build Wms.sln
dotnet test Wms.sln
```

Frontend:

```bash
cd WMS-Frontend
npm run format:check
npm run lint
npm run build
npm run test:ci
```
