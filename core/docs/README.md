# 🏗️ SYNFLOX Core - Domain & Application Layers

> **Projects**: `SYNFLOX.Core.Domain` & `SYNFLOX.Core.Application`  
> **Architecture**: Clean Architecture - Innermost Layers  
> **Last Updated**: December 2025

---

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture Principles](#architecture-principles)
3. [Domain Layer](#domain-layer)
4. [Application Layer](#application-layer)
5. [Entity Reference](#entity-reference)
6. [Service Reference](#service-reference)
7. [DTO Structure](#dto-structure)
8. [ID Encryption System](#id-encryption-system)

---

## 🎯 Project Overview

### What is SYNFLOX Core?

The Core project contains the **shared business logic** used by both Admin API and Client API. It follows Clean Architecture principles where:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      CLEAN ARCHITECTURE LAYERS                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│                    ┌───────────────────────┐                            │
│                    │      Domain Layer     │  ◄── Entities, Enums,     │
│                    │   (Innermost - Core)  │      Interfaces, No deps  │
│                    └───────────┬───────────┘                            │
│                                │                                         │
│                    ┌───────────▼───────────┐                            │
│                    │   Application Layer   │  ◄── DTOs, Services,      │
│                    │   (Depends on Domain) │      Mappers, Interfaces  │
│                    └───────────┬───────────┘                            │
│                                │                                         │
│            ┌───────────────────┴───────────────────┐                    │
│            │                                        │                    │
│  ┌─────────▼─────────┐                ┌────────────▼────────────┐       │
│  │   Admin.WebAPI    │                │    Client.WebAPI        │       │
│  │ (Infrastructure + │                │  (Infrastructure +      │       │
│  │  Presentation)    │                │   Presentation)         │       │
│  └───────────────────┘                └─────────────────────────┘       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
core/
├── SYNFLOX.Core.Domain/           # Domain Layer
│   ├── Entities/                  # Business entities
│   │   ├── Activity/              # ActivityLog
│   │   ├── Authentication/        # Admin, AdminType, RefreshToken, etc.
│   │   ├── Common/                # Base classes (AuditEntity, BaseEntity)
│   │   ├── Licensing/             # Company, CompanyAdmin, LicenseActivation
│   │   ├── Navigation/            # MenuItem
│   │   ├── OnlineAccess/          # OnlineClientToken, OnlineDeviceBinding
│   │   └── Subscriptions/         # Subscription, Plan, Entitlements, etc.
│   ├── Enums/                     # 20 business enums
│   ├── Exceptions/                # Custom exceptions
│   ├── Interfaces/                # Repository interfaces
│   ├── Constants/                 # System constants
│   ├── Helpers/                   # Utility classes
│   └── ValueObjects/              # Value objects
│
└── SYNFLOX.Core.Application/      # Application Layer
    ├── DTOs/                      # Data Transfer Objects (24 folders)
    ├── Services Interfaces/       # 36 service interfaces
    ├── Mapping/                   # AutoMapper profiles & converters
    └── Services/                  # Shared service implementations
```

---

## 🏛️ Architecture Principles

### 1. Dependency Rule

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        DEPENDENCY RULE                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ✅ ALLOWED:                                                            │
│  • Application → Domain                                                 │
│  • Infrastructure → Application → Domain                                │
│  • WebAPI → Infrastructure → Application → Domain                       │
│                                                                          │
│  ❌ FORBIDDEN:                                                          │
│  • Domain → anything                                                    │
│  • Application → Infrastructure                                         │
│  • Application → WebAPI                                                 │
│                                                                          │
│  The Domain layer has ZERO external dependencies.                       │
│  It knows nothing about databases, APIs, or frameworks.                 │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2. Soft Delete Pattern

All entities extend `AuditEntity<TKey>` which provides:

```csharp
public abstract class AuditEntity<TKey> : BaseEntity<TKey>
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

### 3. ID Encryption Rule (CRITICAL)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    🔒 ID ENCRYPTION RULE                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ID ENCRYPTION/DECRYPTION MUST ONLY HAPPEN IN AUTOMAPPER CONVERTERS     │
│  NEVER IN CONTROLLERS, SERVICES, OR ANY OTHER LAYER                    │
│                                                                          │
│  ✅ Correct:                                                            │
│  • AutoMapper converter encrypts entity.Id → dto.Id                     │
│  • AutoMapper converter decrypts request.Id → service parameter         │
│                                                                          │
│  ❌ Forbidden:                                                          │
│  • _idEncryption.Encrypt() in Controller                               │
│  • _idEncryption.Decrypt() in Service                                  │
│  • Injecting IIdEncryptionService outside AutoMapper                   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 Domain Layer

### Entity Categories

#### 1. Authentication Entities (6)

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `Admin` | SYNFLOX system administrator | Username, PasswordHash, TwoFactorEnabled |
| `AdminType` | Role definition | Name, Permissions, IsSuperAdmin |
| `RefreshToken` | JWT refresh tokens | Token, ExpiresAt, IsRevoked |
| `BackupCode` | 2FA backup codes | CodeHash, IsUsed |
| `PasswordResetToken` | Password reset flow | Token, ExpiresAt, OtpCode |
| `SecurityAuditLog` | Security events | Action, IpAddress, UserAgent |

#### 2. Licensing Entities (6)

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `Company` | Tenant/customer organization | Name, ContactEmail, IsActive, TimezoneId |
| `CompanyAdmin` | Client portal admin | Username, PasswordHash, 6 permission flags |
| `CompanyAdminSession` | Admin sessions | SessionId, DeviceHash, ExpiresAt |
| `LicenseActivation` | Device activations | MachineHash, ActivatedAt, HardwareChangeCount |
| `DeviceReplacementRequest` | Device swap requests | OldDeviceHash, NewDeviceHash, Status |
| `OfflineLicenseAdminToken` | Offline swap tokens | TokenHash, ExpiresAt |

#### 3. Subscription Entities (11)

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `SubscriptionPlan` | Product offering | Name, DurationType, MaxDevices, Entitlements |
| `Subscription` | Company's active plan | CompanyId, PlanId, StartDate, ExpiryDate, AccessMode |
| `SubscriptionHistory` | Lifecycle audit | Action, Reason, ChangedBy |
| `PlanEntitlement` | Access rights per plan | ProjectId/ModuleId, AccessLevel, CRUD flags |
| `PlanPrice` | Pricing per currency | Currency, Amount |
| `PlanProject` | Plan ↔ Project link | PlanId, ProjectId |
| `PlanModule` | Plan ↔ Module link | PlanId, ModuleId |
| `Project` | Product/Application | Name, Description, Features |
| `Module` | Feature within product | Name, Description, Features |
| `ProjectModule` | Project ↔ Module link | ProjectId, ModuleId |
| `AccessTimeWindow` | Time-based access | DayOfWeek, StartTime, EndTime |

#### 4. Online Access Entities (3)

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `OnlineClientToken` | API access token | TokenHash, SubscriptionId, Status |
| `OnlineDeviceBinding` | Online device registration | DeviceFingerprint, TokenId |
| `SubscriptionChangeLog` | Pending plan changes | ChangeType, EffectiveDate |

#### 5. Other Entities (4)

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `MenuItem` | Navigation menu | Title, Route, ParentId, Permissions |
| `ActivityLog` | Activity audit | Action, EntityType, EntityId |
| `OutboxEvent` | Async event processing | EventType, Payload, ProcessedAt |
| `PaginationMetadata` | Pagination info | TotalItems, PageNumber, PageSize |

### Enums (20)

| Enum | Values | Purpose |
|------|--------|---------|
| `SubscriptionAccessMode` | None, Full, GracePeriod, ReadOnly, ExportOnly, Blocked | Global subscription access |
| `EntitlementAccessLevel` | None, Full, ReadOnly, ExportOnly, Blocked | Per-entitlement access |
| `PlanDurationType` | Weekly, Monthly, Quarterly, BiAnnual, Yearly, Lifetime | Plan duration |
| `DeviceAdmissionMode` | Open, AdminOnly, AutoWithQueue, HybridAutoAdmin | Device registration policy |
| `ConcurrentAccessMode` | SingleDevice, LimitedConcurrent, Unlimited, TimeBased... | Concurrent access control |
| `DeviceReplacementPolicy` | AdminApproval, AutoReplaceOldest, AutoReplaceLeastActive | Device swap handling |
| `Currency` | USD, EUR, GBP, EGP, SAR, AED, ... | Pricing currencies |
| `UpgradePolicy` | Immediate, Prorated, Deferred, FullReplace | Plan upgrade handling |
| `ClientTokenStatus` | Active, Revoked, Expired, Suspended | Token states |
| `OnlineDeviceStatus` | Active, Suspended, Revoked | Device states |
| `AdminSessionPolicy` | SingleSession, MultiSession | Admin login policy |
| `ChangeEffectPolicy` | Immediate, NextBillingCycle | When changes take effect |
| `ClientSystemType` | Online, Offline | Client system type |
| `LicenseStatus` | Active, Expired, Suspended | Legacy status |
| `OfflineLicenseValidationStatus` | Valid, Expired, ClockTampering, ... | Validation results |
| `SubscriptionEventType` | Created, Renewed, Upgraded, Cancelled, ... | Lifecycle events |
| `ActivityActionType` | Create, Update, Delete, Login, ... | Activity types |
| `AuthProvider` | Local, Google, Microsoft | Login providers |
| `Gender` | Male, Female, Other | Admin profile |
| `ReportType` | PDF, Excel, CSV | Export formats |

---

## 📋 Application Layer

### Service Interfaces (36)

#### Authentication & Security (8)

| Interface | Purpose |
|-----------|---------|
| `IAuthenticationService` | Login, logout, token refresh, 2FA verification |
| `IPasswordResetService` | OTP generation, password reset flow |
| `IBackupCodeService` | Generate, validate, export backup codes |
| `ISecurityAnalyticsService` | Security metrics and trends |
| `IAdvancedSecurityAnalyticsService` | Deep security insights |
| `ISecurityReportService` | Security report generation |
| `ISecurityNotificationService` | Security alert emails |
| `IJwtTokenGenerator` | JWT token creation |

#### Admin Management (4)

| Interface | Purpose |
|-----------|---------|
| `IAdminService` | Admin CRUD operations |
| `IAdminTypeService` | Role CRUD operations |
| `IAdminProfileService` | Profile updates, picture upload |
| `ICurrentUserService` | Get current logged-in admin |

#### Company Management (2)

| Interface | Purpose |
|-----------|---------|
| `ICompanyService` | Company CRUD, activate/deactivate |
| `ICompanyAdminService` | Company admin CRUD, permissions |

#### Subscription System (5)

| Interface | Purpose |
|-----------|---------|
| `ISubscriptionPlanService` | Plan CRUD, pricing, entitlements |
| `ISubscriptionService` | Subscription lifecycle operations |
| `IPlanEntitlementService` | Entitlement management |
| `IProjectService` | Project CRUD |
| `IModuleService` | Module CRUD |

#### Licensing (4)

| Interface | Purpose |
|-----------|---------|
| `IOfflineLicenseService` | Offline license generation/validation |
| `IOfflineLicenseAdminService` | Admin token generation for offline |
| `IOnlineClientService` | Online token management |
| `ICurrencyExchangeService` | Currency conversion |

#### Infrastructure (8)

| Interface | Purpose |
|-----------|---------|
| `IEmailService` | All email operations (30+ methods) |
| `IEmailSender` | Low-level email sending |
| `IFileService` | File operations |
| `IUploadService` | File upload handling |
| `IDownloadService` | File download management |
| `IIdEncryptionService` | ID encryption/decryption |
| `ILocalizationService` | Multi-language support |
| `IPasswordHasher` | Password hashing |

#### Analytics & Search (4)

| Interface | Purpose |
|-----------|---------|
| `IDashboardService` | Dashboard statistics |
| `ISearchService` | Global search |
| `IActivityLogService` | Activity logging |
| `IMenuItemService` | Navigation menu |

---

## 📊 DTO Structure

### DTO Folder Organization (24 folders)

```
DTOs/
├── Admin/                 # Admin management
├── AdminDto/              # Legacy admin DTOs
├── AdminType/             # Role management
├── AdminTypeDto/          # Legacy role DTOs
├── Authentication/        # Login, tokens, 2FA
├── BaseEntityDto/         # Base DTO classes
├── ClientAdmin/           # Client admin management
├── Common/                # Shared DTOs (pagination, etc.)
├── Company/               # Company CRUD
├── CompanyAdmin/          # Company admin CRUD
├── Dashboard/             # Dashboard statistics
├── MenuItem/              # Navigation
├── ModuleDto/             # Module CRUD
├── OfflineLicense/        # Offline license operations
├── OnlineAccess/          # Online token/device
├── PlanDto/               # Plan CRUD
├── PlanEntitlements/      # Entitlement management
├── ProjectDto/            # Project CRUD
├── Responses/             # API response wrappers
├── Search/                # Search results
├── Security/              # Security analytics
└── Subscriptions/         # Subscription lifecycle
```

### DTO Naming Convention

| Type | Naming | Example |
|------|--------|---------|
| Response | `{Entity}Dto` | `CompanyDto`, `SubscriptionDto` |
| Create Request | `Create{Entity}Dto` | `CreateCompanyDto` |
| Update Request | `Update{Entity}Dto` | `UpdateCompanyDto` |
| List Response | `{Entity}ListDto` | `CompanyListDto` |
| Details Response | `{Entity}DetailsDto` | `SubscriptionDetailsDto` |

---

## 🔐 ID Encryption System

### AutoMapper Converters

Two universal converters handle ALL ID encryption/decryption:

#### UniversalEncryptionConverter
```csharp
// Handles Entity → DTO (encrypts IDs for API responses)
Implements:
• IValueConverter<Guid, Guid>           // Single ID
• IValueConverter<Guid?, Guid?>         // Nullable ID
• IValueConverter<Guid, string>         // ID to string
• IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>  // Collections
```

#### UniversalDecryptionConverter
```csharp
// Handles DTO → Entity (decrypts IDs from API requests)
Implements:
• IValueConverter<Guid, Guid>           // Single ID
• IValueConverter<Guid?, Guid?>         // Nullable ID
• IValueConverter<string, Guid?>        // String to ID
• IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>  // Collections
• ITypeConverter<object, Guid>          // Request DTO to ID
• ITypeConverter<object, IEnumerable<Guid>>  // Request DTO to IDs
```

### ID Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ID ENCRYPTION FLOW                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  OUTBOUND (Entity → API Response):                                      │
│  ┌────────────────┐     ┌──────────────────┐     ┌─────────────────┐   │
│  │ Entity         │ ──► │ AutoMapper       │ ──► │ DTO             │   │
│  │ Id: abc-123... │     │ Encrypt(Id)      │     │ Id: xYz7@#...   │   │
│  │ (raw GUID)     │     │ (converter)      │     │ (encrypted)     │   │
│  └────────────────┘     └──────────────────┘     └─────────────────┘   │
│                                                                          │
│  INBOUND (API Request → Service):                                       │
│  ┌────────────────┐     ┌──────────────────┐     ┌─────────────────┐   │
│  │ Request DTO    │ ──► │ AutoMapper       │ ──► │ Decrypted GUID  │   │
│  │ Id: xYz7@#...  │     │ Decrypt(Id)      │     │ abc-123...      │   │
│  │ (encrypted)    │     │ (converter)      │     │ (raw GUID)      │   │
│  └────────────────┘     └──────────────────┘     └─────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 📂 Key Files Reference

### Domain Layer Files

| File | Purpose |
|------|---------|
| `Entities/Common/AuditEntity.cs` | Base entity with soft delete |
| `Entities/Licensing/Company.cs` | Company entity |
| `Entities/Subscriptions/Subscription.cs` | Subscription entity |
| `Entities/Subscriptions/SubscriptionPlan.cs` | Plan entity |
| `Entities/Subscriptions/PlanEntitlement.cs` | Entitlement entity |
| `Enums/SubscriptionAccessMode.cs` | Access mode enum |
| `Interfaces/Repositories/IBaseRepository.cs` | Generic repository interface |

### Application Layer Files

| File | Purpose |
|------|---------|
| `Services Interfaces/ISubscriptionService.cs` | Subscription operations |
| `Services Interfaces/ICompanyService.cs` | Company operations |
| `Services Interfaces/IEmailService.cs` | Email operations |
| `DTOs/Subscriptions/SubscriptionDetailsDto.cs` | Full subscription DTO |
| `DTOs/Company/CompanyDetailsDto.cs` | Full company DTO |
| `Mapping/MappingProfile.cs` | AutoMapper configuration |
| `Mapping/UniversalEncryptionConverter.cs` | ID encryption |
| `Mapping/UniversalDecryptionConverter.cs` | ID decryption |

---

## 🔗 Dependencies

### SYNFLOX.Core.Domain
```xml
<PackageReference Include="System.ComponentModel.Annotations" />
<!-- NO external dependencies - pure .NET -->
```

### SYNFLOX.Core.Application
```xml
<PackageReference Include="AutoMapper" />
<PackageReference Include="FluentValidation" />
<PackageReference Include="Microsoft.Extensions.Localization" />
<!-- References SYNFLOX.Core.Domain -->
```

---

## 🔗 Related Documentation

- **[Admin API](../../admin-api/docs/README.md)** - Admin Portal Backend
- **[Client API](../../client-api/docs/README.md)** - Client Portal Backend
