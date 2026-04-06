# Warehouse Management System

Coursework project with a .NET 8 API, an Angular frontend, and a MySQL database.

## Structure

- `WMS-API` - ASP.NET Core API, application logic, domain model, infrastructure, tests
- `WMS-Frontend` - Angular frontend
- `docs` - design notes and API documentation

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
cd WMS-API/src/Wms.Api
dotnet run
```

Run the frontend:

```bash
cd WMS-Frontend
npm install
npm start
```

The frontend uses a proxy and expects the API on `http://localhost:5021`.

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
