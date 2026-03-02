# Copilot Instructions for DotNetLibrary (.NET 10)

## 1) Architecture to follow

This repository follows a layered architecture. Keep responsibilities separated:

- `DotNetLibrary.Core`
  - Cross-cutting abstractions and implementations (for example `NotificationProcessor`)
  - No persistence logic
- `DotNetLibrary.Data`
  - EF Core entities, `ApplicationDbContext`, repositories, database mappings/migrations
  - No controller logic
- `DotNetLibrary.Domain`
  - Business use-cases/services and orchestration
  - Consume Data and Core contracts through DI
- `DotNetLibrary.Shared`
  - DTOs, request/response contracts, shared constants/options
- `WebApiProject/DotNetCoreWebApi`
  - API controllers, middleware, DI registration, auth/authz wiring

### Dependency direction

Only allow dependencies that keep layers clean:

- `WebApi` -> `Domain`, `Data`, `Core`, `Shared`
- `Domain` -> `Data`, `Core`, `Shared`
- `Data` -> `Shared`
- `Core` -> (no business/data dependency)

Do not introduce circular references.

## 2) Workflow expectations for changes

When implementing changes:

1. Understand impacted layer(s) first.
2. Keep changes minimal and scoped to the requested feature/fix.
3. If schema changes are made:
   - Update entity + `ApplicationDbContext` mapping/indexes/constraints.
   - Add/update repository contract and implementation.
   - Update service contracts/implementations.
   - Update API contracts and controller endpoints only if required.
4. Prefer extending existing patterns over creating parallel patterns.
5. Ensure code compiles before completion.

## 3) Security and authorization requirements

- Keep OAuth/JWT handling standards-compliant.
- Protect endpoints by default; use explicit `[AllowAnonymous]` only where intended (e.g., registration/token endpoints).
- Use policy-based authorization (`AdminOnly`, `AdminOrSelf`, permission policies) instead of inline role checks.
- Never hardcode secrets/keys/passwords in code.
- Read settings from configuration (`appsettings` + environment/secret store).
- For OTP flows:
  - Store hashed OTP values only.
  - Enforce expiration and one-time usage.

## 4) Data and EF Core best practices

- Use explicit table configuration in `ApplicationDbContext`.
- Add proper indexes and uniqueness constraints for lookup/mapping tables.
- Keep many-to-many mapping tables normalized.
- Use soft-delete/audit fields consistently where the model already uses them.
- Keep repository methods async and cancellation-token aware.

## 5) API and domain best practices

- Keep controllers thin; business logic belongs in Domain services.
- Validate requests using model validation and return standard API response contracts.
- Keep DTOs separate from EF entities.
- Prefer explicit, descriptive method names and single-responsibility methods.

## 6) Notification module expectations

- Use `INotificationProcessor` abstraction.
- Route by `CommunicationMedium` (`Email`, `Sms`) via registered channels.
- Email provider must be config-driven (`GmailSmtp` or `AzureEmailService`).
- Keep provider-specific logic inside notification channel/service classes only.

## 7) Quality gate

Before finalizing a task:

- Build must succeed.
- No unrelated refactors.
- No breaking route or contract changes unless explicitly requested.
- Update `README.md` when setup, workflow, or architecture changes significantly.
