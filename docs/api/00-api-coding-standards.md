# 00 — API Coding Standards

Defines coding and design standards for the `WMS-API` solution.
It is tailored to this coursework project and acts as the default development baseline.

---

## 1. Goals

- Keep code readable, testable, and maintainable.
- Enforce consistent conventions across all backend projects.
- Reduce defects by using clear boundaries and predictable patterns.

---

## 2. Solution Boundaries

- `Wms.Domain`: business rules and core entities only.
- `Wms.Application`: use-case orchestration and interfaces.
- `Wms.Infrastructure`: persistence, external integrations, implementations.
- `Wms.Api`: transport layer (HTTP), routing, request/response mapping.
- `Wms.Contracts`: DTOs/contracts used at boundaries.
- `Wms.Common`: shared cross-cutting helpers with low coupling.

Rule: dependencies point inward (API -> Application -> Domain).
Infrastructure depends on Application/Domain abstractions.

---

## 3. Naming Conventions

- Use `PascalCase` for classes, methods, enums, and public members.
- Use `camelCase` for method parameters and local variables.
- Use `_camelCase` for private fields.
- Use intention-revealing names (avoid vague names like `data`, `item2`, `helper`).
- Use one class per file.

Examples:
- `CreatePurchaseOrderCommand`
- `GetLowStockItemsQuery`
- `_unitOfWork`, `_logger`

---

## 4. API Conventions

- Keep route segments lowercase.
- Use nouns for resources, for example:
  - `/api/products`
  - `/api/suppliers`
  - `/api/purchase-orders`
- Return appropriate HTTP codes (`200`, `201`, `204`, `400`, `404`, `409`, `500`).
- Keep controllers thin; move business logic into Application services/handlers.
- Do not return domain entities directly; return contract DTOs.

---

## 5. Code Style and Language Guidelines

- Prefer `var` when type is obvious from the right-hand side.
- Use explicit types when clarity is better than brevity.
- Use `&&` and `||` (not `&` or `|`) for boolean conditions.
- Avoid deep nesting; extract guard clauses and small methods.
- Prefer immutable objects where practical.
- Do not leave dead code, placeholder methods, or unused `using` directives.

---

## 6. Comments and Documentation

- Write comments only when intent is not obvious from code.
- Put comments on their own line.
- Start with uppercase and end with a period.
- Document public API endpoints and non-obvious business rules.
- Do not leave open TODOs unless linked to a ticket/reference.

---

## 7. Validation and Error Handling

- Validate input at API boundary and use-case boundary.
- Fail fast with clear validation messages.
- Use a global exception handler in API layer.
- Avoid empty `catch` blocks.
- Log exceptions with context (operation, IDs, actor where relevant).

---

## 8. Logging Standards

Use structured logging with categories:
- `WMS Audit`: user/business actions (information level).
- `WMS Error`: application or configuration failures.

Minimum log fields:
- Timestamp
- Level
- Source context
- Message
- Correlation/request ID (if available)

Suggested audit message format:
`[WMS Audit] [userId] [action] [outcome]`

Suggested error message format:
`[WMS Error] [source] [details]`

---

## 9. Testing Standards

- Test names should describe behavior and expected outcome.
- Unit tests must assert business rules, not placeholder truths.
- Cover success path, validation failures, and edge cases.
- Keep tests independent and deterministic.
- Add integration tests for persistence behavior when EF Core is introduced.

---

## 10. Persistence Standards (MySQL + EF Core)

Folder naming recommendation in `Wms.Infrastructure`:
- `Persistence/` for DbContext, configurations, migrations, repositories.

Why `Persistence` over `Data` or `Models`:
- `Persistence` describes storage concerns.
- `Data` is broad and ambiguous.
- `Models` often conflicts with domain models/DTO models naming.

Recommended structure:
- `Persistence/WmsDbContext.cs`
- `Persistence/Configurations/`
- `Persistence/Migrations/`
- `Persistence/Repositories/`

---

## 11. Package and Dependency Management

- Keep package versions consistent across projects.
- Prefer central package management (`Directory.Packages.props`) when multiple
  projects share dependencies.
- Remove unused packages promptly.

---

## 12. Definition of Done (Backend)

- Feature implemented in correct layer.
- Validation and error handling included.
- Unit tests added/updated and passing.
- API contract/docs updated where needed.
- No new analyzer warnings introduced.
- No generated artifacts committed (`bin/`, `obj/`, coverage, etc.).
