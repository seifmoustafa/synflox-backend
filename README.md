# SYNFLOX Backend - Complete Technical Documentation

> **Comprehensive technical reference for developers, architects, and DevOps engineers**  
> **Version:** 2.0 | **Updated:** November 16, 2025

---

## 📋 Quick Navigation

- [Architecture](#architecture) - Clean Architecture layers and principles
- [Technology Stack](#technology-stack) - All frameworks and libraries used
- [Domain Entities](#domain-entities) - All 20 business entities documented
- [API Endpoints](#api-endpoints) - All 17 controllers with complete endpoint reference
- [Services](#services) - All 27 service implementations
- [Background Jobs](#background-jobs) - Automated tasks and scheduling
- [Security](#security) - Authentication, authorization, encryption
- [Database](#database) - Schema, migrations, configuration
- [Development](#development) - Setup, workflow, testing
- [Deployment](#deployment) - Production setup and best practices

---

## 🏗️ Architecture

### Clean Architecture Implementation

SYNFLOX follows **Clean Architecture** with strict separation:

```
┌─────────────────────────────────────────────────────┐
│                  WebAPI Layer                        │
│  • 17 REST Controllers                               │
│  • JWT Authentication                                │
│  • Exception Handling                                │
│  • Localization (EN/AR)                              │
│  • CORS & Swagger                                    │
└─────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────┐
│               Application Layer                      │
│  • Service Interfaces                                │
│  • 50+ DTOs                                          │
│  • AutoMapper Profiles                               │
│  • Universal Encryption Converters                   │
└─────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────┐
│                Domain Layer                          │
│  • 20 Entities                                       │
│  • 15+ Enums                                         │
│  • Business Rules                                    │
│  • ⚠️ ZERO Dependencies                              │
└─────────────────────────────────────────────────────┘
                      ↑
┌─────────────────────────────────────────────────────┐
│             Infrastructure Layer                     │
│  • EF Core 8.0                                       │
│  • 27 Services                                       │
│  • Repositories                                      │
│  • Background Jobs                                   │
└─────────────────────────────────────────────────────┘
```

### Key Principles

- **Dependency Inversion** - Domain has zero dependencies
- **Separation of Concerns** - Each layer has single responsibility
- **SOLID Principles** - Clean, maintainable code

---

## 🔧 Technology Stack

**Framework:**
- .NET 8.0 (LTS)
- ASP.NET Core 8.0
- C# 12

**Data:**
- Entity Framework Core 8.0
- SQL Server (primary)
- Oracle (secondary support)

**Security:**
- JWT Authentication
- BCrypt.Net (password hashing)
- AES-256 + HMAC SHA256

**Background Jobs:**
- Hangfire

**Logging:**
- Serilog

**Mapping:**
- AutoMapper 12.0

**Email:**
- MailKit + MimeKit

**Documentation:**
- Swagger/OpenAPI 3.0

---

## 📦 Domain Entities

### Complete Entity List (20 Total)

#### **Authentication (3)**
1. **Admin** - System administrators
2. **AdminType** - Roles (SuperAdmin, Admin)
3. **RefreshToken** - JWT refresh tokens

#### **Client Access (2)**
4. **ClientAccessToken** - External system tokens
5. **ClientTokenUsageLog** - Token usage audit

#### **Licensing (1)**
6. **Company** - Customer companies

#### **Subscriptions (9)** 🆕
7. **SubscriptionPlan** - Plans with duration types
8. **Subscription** - Active subscriptions
9. **Project** - Products (ERP, CRM, etc.)
10. **Module** - Feature modules
11. **ProjectModule** - Project ↔ Module link
12. **PlanProject** - Plan ↔ Project link
13. **PlanModule** - Plan ↔ Module link
14. **PlanPrice** - Multi-currency pricing
15. **OutboxEvent** - Event sourcing

#### **Navigation (1)**
16. **MenuItem** - Dynamic navigation

#### **Base Classes (4)**
17. **BaseEntity<TKey>** - Base with ID
18. **AuditEntity<TKey>** - Base with audit trail
19. **IBaseEntity<TKey>** - Entity interface
20. **PaginationMetadata** - Pagination info

### Key Entity Details

#### **SubscriptionPlan** 🆕⭐
```csharp
public class SubscriptionPlan : AuditEntity<Guid>
{
    public string Name { get; set; }
    
    // ⭐ NEW: Dynamic Duration
    public PlanDurationType DurationType { get; set; }
    public int DurationMonths { get; set; }
    
    public bool AllowTrial { get; set; }
    public bool AutoRenew { get; set; }
    
    // ⭐ Computed Property
    public bool IsLifetimePlan => DurationType == PlanDurationType.Lifetime;
}
```

**Duration Types:**
- Weekly (7 days)
- BiWeekly (14 days)
- Monthly (1 month)
- Quarterly (3 months)
- SemiAnnually (6 months)
- Yearly (12 months)
- Biennial (2 years)
- Triennial (3 years)
- **Lifetime (Never expires)** 🆕

#### **Subscription** 🆕⭐
```csharp
public class Subscription : AuditEntity<Guid>
{
    public DateTime ExpiryDateUtc { get; set; } // DateTime.MaxValue for lifetime
    public bool IsActive { get; set; }
    public bool AutoRenew { get; set; }
    
    // Offline license key
    public string? OfflineLicenseKey { get; set; }
    
    // ⭐ Computed Properties
    public bool IsExpired => DateTime.UtcNow > ExpiryDateUtc;
    public bool IsLifetime => Plan?.DurationType == PlanDurationType.Lifetime;
}
```

---

## 🎛️ API Endpoints

### Complete Controller List (17 Total)

#### **1. AdminAuthenticationController**
`/api/admin/auth`

```http
POST   /login        # Admin login (JWT)
POST   /refresh      # Refresh token
POST   /logout       # Revoke token
```

#### **2. AdminsController**
`/api/admins` | Auth: SuperAdminOnly

```http
GET    /             # List (paginated, searchable)
GET    /{id}         # Get by ID
POST   /             # Create
PUT    /{id}         # Update
DELETE /{id}         # Soft delete
PUT    /{id}/change-password
```

#### **3. CompanyController**
`/api/companies` | Auth: SuperAdminOnly

```http
GET    /             # List (filters: isActive, search)
GET    /{id}         # Get details
POST   /             # Create
PUT    /{id}         # Update
DELETE /{id}         # Delete
```

#### **4. SubscriptionsController** 🆕⭐
`/api/subscriptions` | Auth: SuperAdminOnly

**Complete Lifecycle Management:**

```http
# CRUD
GET    /                         # List all
GET    /{id}                     # Get details
POST   /                         # Create
DELETE /{id}                     # Cancel

# Lifecycle
PUT    /{id}/suspend             # Suspend
PUT    /{id}/resume              # Resume
PUT    /{id}/pause               # Pause (hold time)
PUT    /{id}/unpause             # Unpause
PUT    /{id}/extend              # Extend expiry
PUT    /{id}/stop-trial          # Trial → Paid
PUT    /{id}/reactivate          # Reactivate

# Advanced
POST   /{id}/upgrade             # Upgrade plan
POST   /{id}/renew               # Renew

# Status (Public)
GET    /{id}/status              # Check status
```

**Lifetime Plan Rules:**
- ✅ Cannot create with trial
- ✅ Cannot enable auto-renew
- ✅ Cannot schedule upgrade
- ✅ Cannot extend (already permanent)
- ✅ Cannot renew (already permanent)
- ✅ CAN suspend/resume manually

#### **5. PlansController** 🆕⭐
`/api/subscription-plans` | Auth: SuperAdminOnly

```http
GET    /             # List all plans
GET    /{id}         # Get plan details
POST   /             # Create plan
PUT    /{id}         # Update plan
DELETE /{id}         # Delete plan
```

**Create Lifetime Plan Example:**
```json
POST /api/subscription-plans
{
  "name": "Enterprise Lifetime",
  "durationType": 99,  // Lifetime
  "allowTrial": false,
  "autoRenew": false,
  "prices": [
    { "currency": 1, "amount": 9999.99 }
  ]
}
```

#### **6-8. Configuration Controllers**
- **ProjectsController** - `/api/projects`
- **ModulesController** - `/api/modules`
- **MenuItemController** - `/api/menu-items`

#### **9. ClientTokenController** 🆕
`/api/client-tokens` | Auth: SuperAdminOnly

```http
GET    /                         # List tokens
POST   /                         # Generate token
PUT    /{id}/revoke              # Revoke
GET    /{id}/usage-logs          # Usage history
```

**Purpose:** Generate JWT tokens for external systems (ERP, CRM)

#### **10. ClientApiController** 🆕
`/api/client` | Auth: Client JWT Token

```http
GET    /subscription/{id}/status      # Check status
POST   /validate-license-key          # Validate key
GET    /company/{id}/details          # Company info
```

**Usage:** External products call these endpoints

#### **11. LicenseController** 🆕
`/api/licenses`

```http
POST   /subscriptions/{id}/generate-key    # Generate
POST   /subscriptions/{id}/regenerate-key  # Regenerate
POST   /validate-key                       # Validate (public)
```

**License Key Security:**
- AES-256 encryption
- HMAC SHA256 signature
- Clock tampering detection

#### **12. DashboardController** 🆕
`/api/dashboard` | Auth: SuperAdminOnly

```http
GET    /                    # Full dashboard
GET    /companies           # Company stats
GET    /subscriptions       # Subscription stats
GET    /admins              # Admin stats
GET    /alerts              # System alerts
```

#### **13-17. Supporting Controllers**
- **SearchController** - Global search
- **UploadsController** - File uploads
- **DownloadsController** - Export data
- **CustomEmailController** - Emails
- **AdminTypesController** - Roles

---

## ⚙️ Services

### Complete Service List (27 Total)

#### **Core Business Services (10)**
1. **SubscriptionService** 🆕 - Subscription lifecycle
2. **SubscriptionPlanService** 🆕 - Plan management
3. **CompanyService** - Company CRUD
4. **AdminService** - Admin management
5. **ProjectService** - Project CRUD
6. **ModuleService** - Module CRUD
7. **ClientTokenService** 🆕 - Token management
8. **LicenseService** 🆕 - License keys
9. **DashboardService** 🆕 - Dashboard data
10. **SearchService** - Global search

#### **Authentication & Authorization (3)**
11. **AuthenticationService** - Login, tokens
12. **ClientJwtService** 🆕 - Client tokens
13. **CurrentUserService** - User context

#### **Infrastructure Services (8)**
14. **EmailService** - SMTP email
15. **EmailQueue** - Email queue
16. **FileService** - File operations
17. **UploadService** - File uploads
18. **DownloadService** - Data export
19. **LocalizationService** - i18n
20. **IdEncryptionService** - ID encryption
21. **MenuItemService** - Navigation

#### **Utility Services (6)**
22. **AdminTypeService** - Role management
23. **ClientApiService** 🆕 - Client API
24. **UploadCleanupService** - Cleanup
25. **EmailLocalizationHelper** - Email templates
26. **EmailSender** - Email sending
27. **PlanDurationHelper** 🆕 - Duration calculations

### Key Service: SubscriptionService 🆕

**Lifetime Plan Logic Example:**
```csharp
public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
{
    var plan = await _planRepository.GetByIdAsync(dto.PlanId);
    
    // ⭐ Lifetime Validation
    if (plan.IsLifetimePlan)
    {
        if (dto.StartWithTrial)
            throw new BadRequestException("Lifetime plans cannot have trials");
        
        if (dto.AutoRenew)
            throw new BadRequestException("Lifetime plans cannot auto-renew");
    }
    
    // ⭐ Calculate expiry using helper
    var expiryDate = PlanDurationHelper.CalculateExpiryDate(
        startDate,
        plan.DurationType
    ); // Returns DateTime.MaxValue for Lifetime
    
    // Create subscription...
}
```

---

## 🔄 Background Jobs

### SubscriptionStatusBackgroundJob 🆕⭐
**Schedule:** Every 10 minutes  
**Purpose:** Automated subscription management

#### **Tasks:**

**1. Process Expired Subscriptions**
```csharp
// Mark expired (SKIP lifetime plans)
var expired = subscriptions.Where(s => 
    !s.Plan.IsLifetimePlan &&  // ⭐ SKIP
    s.ExpiryDateUtc + GracePeriod < DateTime.UtcNow
);
```

**2. Process Auto-Renewals**
```csharp
// Auto-renew (SKIP lifetime plans)
var renewals = subscriptions.Where(s =>
    s.AutoRenew &&
    !s.Plan.IsLifetimePlan &&  // ⭐ SKIP
    s.ExpiryDateUtc <= DateTime.UtcNow.AddDays(7)
);
```

**3. Process Deferred Upgrades**
```csharp
// Activate scheduled upgrades
var upgrades = subscriptions.Where(s =>
    s.NextPlanId != null &&
    s.NextPlanStartDateUtc <= DateTime.UtcNow
);
```

**Logging:**
```csharp
_logger.LogInformation(
    "Skipping lifetime subscription {Id} from {Process}",
    subscription.Id,
    "expiry processing"
);
```

---

## 🔐 Security

### ID Encryption Pattern ⚠️ **CRITICAL RULE**

**Rule:** Encryption/decryption ONLY in AutoMapper converters.

**Universal Converters:**

```csharp
// UniversalEncryptionConverter
Guid → Guid (encrypted)
Guid? → Guid?
IEnumerable<Guid> → IEnumerable<Guid>

// UniversalDecryptionConverter
Guid → Guid (decrypted)
Guid? → Guid?
Request DTOs → Guid (smart extraction)
```

**Usage:**
```csharp
CreateMap<Company, CompanyDto>()
    .ForMember(d => d.Id, 
        opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id));
```

**Forbidden:**
```csharp
// ❌ NEVER in controllers or services
var decryptedId = _idEncryption.Decrypt(id);  // WRONG!

// ✅ ALWAYS use AutoMapper
var dto = _mapper.Map<CompanyDto>(entity);  // CORRECT!
```

### Authentication

**Admin JWT:**
- Access token: 30 minutes
- Refresh token: 60 days
- BCrypt password hashing

**Client JWT:**
- Separate tokens for external systems
- Configurable expiration
- Endpoint restrictions

### License Key Security

**Format:**
```
Base64(AES-256(JSON + HMAC-SHA256))
```

**Features:**
- AES-256 encryption
- HMAC tamper detection
- Clock tampering detection
- Version control

---

## 💾 Database

### Schema Overview

**Tables:** 20+ tables with proper indexing

**Key Tables:**
- `Companies` - Customer tenants
- `Subscriptions` - Active subscriptions
- `SubscriptionPlans` - Commercial plans
- `Admins` - System administrators
- `ClientAccessTokens` - External tokens

### Migration: Add DurationType 🆕

**SQL:**
```sql
-- Add DurationType column
ALTER TABLE SubscriptionPlans
ADD DurationType INT NOT NULL DEFAULT 3; -- Monthly

-- Migrate existing plans
UPDATE SubscriptionPlans
SET DurationType = CASE DurationMonths
    WHEN 1 THEN 3   -- Monthly
    WHEN 3 THEN 4   -- Quarterly
    WHEN 6 THEN 5   -- SemiAnnually
    WHEN 12 THEN 6  -- Yearly
    WHEN 24 THEN 7  -- Biennial
    WHEN 36 THEN 8  -- Triennial
    ELSE 3          -- Default to Monthly
END
WHERE DurationMonths > 0;

-- Lifetime plans
UPDATE SubscriptionPlans
SET DurationType = 99,  -- Lifetime
    DurationMonths = 0
WHERE DurationMonths = 0;
```

### Connection Strings

**SQL Server:**
```json
"SqlServerConnection": "Server=.\\SQLEXPRESS;Database=SYNFLOX;Trusted_Connection=True;"
```

**Oracle:**
```json
"OracleConnection": "Data Source=localhost:1521/SYNFLOX;User Id=admin;Password=***;"
```

---

## 🚀 Development

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB/Express/Full)
- Visual Studio 2022 or VS Code

### Setup

**1. Clone & Restore:**
```bash
git clone <repo>
cd SYNFLOX
dotnet restore
```

**2. Configure Database:**
Edit `appsettings.json` in WebAPI project.

**3. Run Migrations:**
```bash
cd WebAPI
dotnet ef database update
```

**4. Run:**
```bash
dotnet run --project WebAPI
```

**5. Access:**
- Swagger: `https://localhost:5001/swagger`
- Login: username: `superadmin`, password: `password`

### Development Workflow

**Add New Feature:**
1. Create entity in `Domain/Entities`
2. Add repository interface in `Domain/Interfaces`
3. Create DTOs in `Application/DTOs`
4. Add AutoMapper profile
5. Implement repository in `Infrastructure`
6. Create service interface + implementation
7. Add controller in `WebAPI`
8. Create migration

**Testing:**
```bash
dotnet test
```

---

## 📦 Deployment

### Docker

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

**Build:**
```bash
docker build -t synflox-api .
docker run -p 5000:80 synflox-api
```

### Configuration

**Production Settings:**
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=prod;Database=SYNFLOX;User=***"
  },
  "JwtSettings": {
    "SecretKey": "production-secure-key-256-bits",
    "Lifetime": 15
  },
  "LicenseKeySettings": {
    "EncryptionKey": "base64-key",
    "SigningKey": "base64-key"
  }
}
```

---

## 📊 Features Summary

### ✅ Implemented Features

**Authentication:**
- JWT admin authentication
- JWT client tokens
- Refresh token rotation
- Role-based authorization

**Subscription Management:** 🆕
- 9 duration types + Lifetime
- Complete lifecycle (create, suspend, resume, extend, renew, upgrade)
- Trial periods
- Auto-renewal
- Scheduled upgrades
- Prorated billing

**License Management:**
- Offline license keys
- AES-256 encryption
- Clock tampering detection
- Key regeneration

**Multi-Product Support:**
- Projects (products)
- Modules (features)
- Flexible plan bundles
- Multi-currency pricing

**Background Jobs:**
- Expiry processing
- Auto-renewals
- Deferred upgrades
- Email notifications

**Dashboard:**
- Real-time statistics
- Company metrics
- Subscription metrics
- System alerts

**Localization:**
- English + Arabic
- RTL/LTR support
- Resource-based i18n

---

## 📞 Support

**Documentation:**
- Business: `/README.md`
- Backend: `/SYNFLOX/README.md` (this file)
- Frontend: `/synflox-frontend/README.md`

**API Documentation:**
- Swagger UI: `https://localhost:5001/swagger`

---

## 🎯 Quick Reference

**Project Structure:**
```
SYNFLOX/
├── Domain/              # 20 entities, enums, helpers
├── Application/         # DTOs, interfaces, mapping
├── Infrastructure/      # Services, repositories, EF
├── WebAPI/              # 17 controllers
└── README.md            # This file
```

**Key Files:**
- `Domain/Entities/Subscriptions/SubscriptionPlan.cs` - Plans with duration types
- `Domain/Entities/Subscriptions/Subscription.cs` - Subscriptions with lifetime support
- `Domain/Enums/PlanDurationType.cs` - Duration type enum
- `Domain/Helpers/PlanDurationHelper.cs` - Duration calculations
- `Infrastructure/Services/SubscriptionService.cs` - Subscription logic
- `Infrastructure/BackgroundJobs/SubscriptionStatusBackgroundJob.cs` - Automated tasks

**Important Enums:**
- `PlanDurationType` - Weekly, Monthly, Yearly, **Lifetime**, etc.
- `UpgradePolicy` - ExtendInPlace, CreateFollowUp, FullReplace, etc.
- `Currency` - USD, EUR, EGP, SAR, AED
- `ClientTokenStatus` - Active, Revoked, Expired

---

<div align="center">

**SYNFLOX Backend**  
*Enterprise Central Licensing System*

**Version 2.0** | **November 2025**

</div>
