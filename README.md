# COM5043 Warehouse Management System

## Overview

This repository contains an **object‑oriented Warehouse Management System (WMS)** developed for the COM5043 Object Oriented Programming module. The system is designed to demonstrate clean architecture, strong typing, automated testing, and industry‑standard DevOps practices.

The solution is split into a **C# ASP.NET Core backend** and an **Angular (TypeScript) frontend**, with full CI, static analysis, and containerisation.

---

## Technology Stack

### Backend

* **Language**: C# (.NET 8)
* **Framework**: ASP.NET Core Web API
* **Architecture**: Layered / Clean Architecture
* **Testing**: xUnit
* **Static Analysis**: StyleCop Analyzers, SonarCloud
* **Containerisation**: Docker & Docker Compose

### Frontend

* **Framework**: Angular
* **Language**: TypeScript (strict mode)
* **Styling**: SCSS
* **Testing**: Jasmine + Karma
* **Formatting**: Prettier
* **Linting**: ESLint
* **Code Coverage**: Karma coverage reports

### DevOps / Tooling

* GitHub Actions (CI pipelines)
* SonarCloud (quality gates & trend analysis)
* Docker (reproducible backend runtime)

---

## Repository Structure

```
COM5043_Warehouse-Management-system/
│
├─ .github/workflows/
│  ├─ backend.yml        # CI: build & test .NET backend
│  ├─ frontend.yml       # CI: format, lint, test, coverage, build Angular
│  └─ sonar.yml          # CI: SonarCloud static analysis
│
├─ WMS-API/               # Backend (ASP.NET Core)
│  ├─ src/
│  │  ├─ Wms.Api/         # API layer (controllers, Program.cs, Swagger)
│  │  ├─ Wms.Application/ # Application services & use cases
│  │  ├─ Wms.Domain/      # Core domain models and business rules
│  │  ├─ Wms.Infrastructure/ # Data access & external concerns
│  │  ├─ Wms.Contracts/   # DTOs and API contracts
│  │  └─ Wms.Common/      # Shared utilities
│  │
│  ├─ tests/
│  │  ├─ Wms.Domain.Tests/
│  │  └─ Wms.Application.Tests/
│  │
│  ├─ docker/
│  │  ├─ Dockerfile.api
│  │  └─ docker-compose.yml
│  │
│  ├─ Wms.sln
│  └─ global.json         # SDK pin with roll‑forward enabled
│
├─ WMS-Frontend/          # Angular frontend
│  ├─ src/
│  ├─ coverage/
│  ├─ angular.json
│  ├─ package.json
│  └─ karma.conf.js
│
├─ sonar-project.properties
└─ README.md
```

---

## Backend Design

* **Domain‑driven structure** separating business rules from infrastructure
* API layer is **decoupled** from domain and application logic
* Strong use of **encapsulation, abstraction, and separation of concerns**
* Swagger enabled for API documentation and manual testing
* Dockerised for consistent local and CI execution

---

## Frontend Design

* Angular application used purely as a **UI driver** for backend functionality
* Business logic remains in the backend, aligning with module guidance
* Strict TypeScript configuration to minimise runtime errors
* Automated formatting, linting, and testing enforced via CI

---

## Testing Strategy

### Backend

* Unit tests for **Domain** and **Application** layers
* Tests run locally, in Docker, and in CI

### Frontend

* Component and configuration tests using Jasmine and Karma
* Code coverage generated in CI mode

---

## Continuous Integration

Each push and pull request triggers:

* Backend build and tests
* Frontend formatting, linting, tests, and coverage
* SonarCloud analysis for both C# and TypeScript

This ensures consistent quality checks and enables **trend‑based reflection** in the final report.

---

## Module Alignment

This project demonstrates:

* **LO1**: Application of object‑oriented principles with backend focus
* **LO2**: Mapping of OO design to concrete implementation
* **LO3**: Use of encapsulation, inheritance, and polymorphism
* **LO4**: Use of CASE tools (CI, static analysis, automated testing)

---
