ADR-0001 — Layered / Clean Architecture
# ADR-0001: Use Layered / Clean Architecture

## Status
Accepted

## Context
The system must be maintainable and testable. The user interface must be
decoupled from business logic, as required by the coursework brief.

The system is expected to grow in complexity. Clear separation of concerns is required.

## Decision
The backend will use a layered (Clean Architecture–style) structure:

- API layer handles HTTP transport and DTO mapping
- Application layer orchestrates use-cases
- Domain layer contains business rules and entities
- Infrastructure layer contains persistence and external concerns

Dependencies point inward only.

## Consequences
- Business logic is isolated and testable
- UI and database can change without impacting core rules
- Slightly more upfront structure with reduced long-term complexity

## Alternatives Considered
- Monolithic service layer (rejected: poor testability)
- Microservices (rejected: unnecessary complexity for coursework scope)

ADR-0002 — Domain Model with Aggregates
# ADR-0002: Domain Model with Aggregates

## Status
Accepted

## Context
The system requires consistent enforcement of business rules such as stock
levels, order state transitions, and financial transactions.

These rules must not be bypassed by API or persistence logic.

## Decision
A domain model is used with:
- Entities for identity-based objects
- Value objects for immutable concepts
- Aggregate roots (e.g. PurchaseOrder, CustomerOrder) enforcing invariants

Business rules are implemented inside domain methods where appropriate.

## Consequences
- Invalid states cannot be created accidentally
- Domain logic is explicit and testable
- More design effort than an anemic model

## Alternatives Considered
- Anemic domain model (rejected: weak OO evidence)

ADR-0003 — Stock Movement Audit Model
# ADR-0003: Stock Movement Audit Model

## Status
Accepted

## Context
Inventory changes must be traceable for auditing and financial reporting.
Stock must never silently change without record.

## Decision
All inventory changes are recorded as StockMovement entities.
Current stock quantity is derived or cached and supported by audit data.

## Consequences
- Full inventory audit trail
- Easier debugging and reporting
- Slight increase in data volume

## Alternatives Considered
- Direct quantity updates only (rejected: no audit trail)

ADR-0004 — Relational Database with EF Core (MySQL)
# ADR-0004: Relational Database with EF Core and MySQL

## Status
Accepted

## Context
The system requires structured relationships, transactions, and strong data
integrity.

The technology stack should reflect industry practice and be well supported.

## Decision
Use MySQL as the relational database with Entity Framework Core for persistence.

Domain entities are mapped via explicit configuration classes.

## Consequences
- Strong referential integrity
- Familiar tooling and migration support
- ORM mapping complexity managed via configuration

## Alternatives Considered
- In-memory storage (rejected: insufficient realism)
- NoSQL database (rejected: unnecessary for relational data)

ADR-0005 — DTO Boundary at API Layer
# ADR-0005: DTO Boundary at API Layer

## Status
Accepted

## Context
The API must not expose domain entities directly to avoid tight coupling and
accidental rule bypass.

The frontend and backend must evolve independently.

## Decision
All API requests and responses use DTOs defined in Wms.Contracts.
Domain entities are mapped internally.

## Consequences
- Clear API contracts
- Improved validation and security
- Additional mapping code required

## Alternatives Considered
- Exposing domain entities (rejected: tight coupling)

ADR-0006 — File-Based Report Export
# ADR-0006: File-Based Report Export

## Status
Accepted

## Context
The coursework requires report export functionality. External integrations
and cloud storage are out of scope.

## Decision
Reports are exported as TXT or JSON files stored locally on the server.
Each export is logged via a ReportExport record.

## Consequences
- File-based export mechanism with a testable path
- No external dependencies
- Suitable for local and coursework usage

## Alternatives Considered
- Cloud storage export (rejected: out of scope)
- Email export (rejected: unnecessary complexity)
