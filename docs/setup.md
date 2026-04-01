# Setup and Run Guide

Shows how to run the API and frontend locally for coursework demos.

## Prerequisites
- .NET SDK 8
- Node.js 20 LTS with npm 10+
- MySQL 8 locally or Docker Desktop

## Backend (API)
From repository root:

```bash
cd WMS-API
dotnet restore
dotnet build
dotnet run --project src/Wms.Api
```

The API prints its listening URLs on startup.

### Database
Connection string key: `ConnectionStrings:DefaultConnection` in
`WMS-API/src/Wms.Api/appsettings.json`.

To run the database with Docker:

```bash
docker compose -f WMS-API/docker/docker-compose.yml up --build
```

## Frontend (Angular)
From repository root:

```bash
cd WMS-Frontend
npm install
npm start
```

The Angular dev server runs on port 4200 by default.

## Tests (Optional)

```bash
cd WMS-API
dotnet test
```

```bash
cd WMS-Frontend
npm run test:ci
```
