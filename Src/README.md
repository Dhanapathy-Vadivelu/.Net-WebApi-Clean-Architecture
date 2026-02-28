# DotNetLibrary Web API

Enterprise-ready `.NET 10` Web API with layered architecture, OAuth token issuance, permission-based authorization, OTP-enabled user registration, and scalable lookup/mapping design.

## Current project structure

```
Src/
├─ DotNetLibrary.Core/
│  └─ NotificationProcessor/
│     ├─ Contracts/                # Notification abstractions
│     ├─ Models/                   # Medium/message/options
│     └─ Services/                 # Email (Gmail/Azure), SMS, processor
├─ DotNetLibrary.Data/
│  ├─ Entities/                    # User, Role, Permission, mappings, UserOtp
│  ├─ Contracts/                   # Repository contracts
│  ├─ Repository/                  # EF Core repository implementations
│  ├─ Migrations/                  # EF migrations
│  └─ ApplicationDbContext.cs      # EF model and mappings
├─ DotNetLibrary.Domain/
│  ├─ Contracts/                   # Business service contracts
│  └─ Services/                    # User/Auth/Lookup/Role-Permission services
├─ DotNetLibrary.Shared/
│  ├─ Payload/                     # Requests and DTOs
│  ├─ Responses/                   # API response models
│  └─ Security/                    # OAuth and authorization policy constants
└─ WebApiProject/DotNetCoreWebApi/
   ├─ Controllers/                 # API endpoints
   ├─ Authorization/               # Custom handlers/requirements
   ├─ Middleware/                  # Exception middleware
   ├─ Program.cs                   # DI, authN/authZ, pipeline
   └─ appsettings.json             # DB, OAuth, Notification config
```

## Features

- OAuth 2.0 style token endpoint (`password` + `external` grant types)
- JWT authentication and fallback authorization policy
- Role + permission model (`Role`, `Permission`, `RolePermissionMap`)
- Policy-based authorization (`AdminOnly`, `AdminOrSelf`, permission policies)
- User registration with OTP email verification
- User activation flow (`IsActive`, `EmailVerifiedAt`) and OTP lifecycle
- Notification module supporting multiple communication mediums:
  - Email: `Gmail SMTP` or `Azure Email Service`
  - SMS: pluggable channel with feature toggle
- Lookup APIs for role and permission data
- Role-permission management APIs
- Global exception handling middleware

## Database

### Main entities

- `Users`
- `Roles`
- `Permissions`
- `UserRoleMaps`
- `RolePermissionMaps`
- `UserOtps`

### EF migration commands (PowerShell)

Run from workspace root:

```powershell
dotnet restore
dotnet build

dotnet ef migrations add InitialCreate -p DotNetLibrary.Data -s WebApiProject\DotNetCoreWebApi --context ApplicationDbContext
dotnet ef database update -p DotNetLibrary.Data -s WebApiProject\DotNetCoreWebApi --context ApplicationDbContext
```

If `dotnet-ef` is missing:

```powershell
dotnet tool install --global dotnet-ef
```

## Best practices followed

- Layered architecture with clear separation of concerns (`Core`, `Data`, `Domain`, `Shared`, `WebApi`)
- Dependency injection for all key services/repositories
- Policy and claim-based authorization (no hardcoded endpoint checks)
- Centralized security configuration using options binding
- Standardized API response contracts
- Repository abstraction for persistence concerns
- DTO/request contracts separated from entities
- OTPs stored as hashes, not plain text
- Logging for key business/security operations
- Config-driven external integrations (OAuth, SMTP/Azure email)
- Extensible notification strategy via medium-based channel abstraction

## Configuration notes

- Set a strong `OAuth:SigningKey` in `appsettings.json`.
- Configure `Notification:Email:Provider` as either:
  - `GmailSmtp`
  - `AzureEmailService`
- Fill corresponding provider credentials before using OTP email flow.
