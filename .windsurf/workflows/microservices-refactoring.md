---
description: Refactor backend to true microservices - BLOCKED by circular dependencies
---

# SYNFLOX Microservices Refactoring Plan

## ⚠️ STATUS: BLOCKED

**Attempted:** December 14, 2025  
**Result:** Failed after 128+ errors due to tight coupling  
**Current State:** Option A (Simplified) - Working with 0 errors

---

## Root Cause Analysis

### Critical Circular Dependencies Found:

1. **OfflineLicenseAdminService** (Admin) → **ClientAdmin DTOs** (Client)
   - Admin service manages client admin tokens
   - References: ClientAdminContext, BindDeviceRequest, DeviceBindingResponse

2. **InfrastructureServiceRegistration** → **ALL services**
   - Single file registers all 40+ services
   - Cannot split without major refactoring

3. **EmailService** → **Company/Subscription DTOs**
   - Email service references domain-specific DTOs
   - Used by both Admin and Client systems

4. **Background Jobs** → **Domain Services**
   - OutboxProcessorBackgroundJob → IEmailService
   - ExportFilesCleanupJob → IFileHostExportService
   - All jobs registered in single Core file

---

## What Would Be Required (Estimated 2-3 days)

### Phase 1: Break Dependencies (8 hours)
1. Create shared License DTOs in Core
2. Split IEmailService into IEmailSenderBase (Core) and IEmailService (Admin)
3. Move Background Jobs to appropriate APIs
4. Create AdminServiceRegistration.cs and ClientServiceRegistration.cs

### Phase 2: Create Projects (4 hours)
1. Admin.Application with Admin-specific DTOs
2. Admin.Infrastructure with Admin-specific Services
3. Client.Application with Client-specific DTOs
4. Client.Infrastructure with Client-specific Services

### Phase 3: Split Service Registration (4 hours)
1. Core.InfrastructureServiceRegistration → only shared (DbContext, Encryption, etc.)
2. Admin.ServiceRegistration → Admin services (Auth, Company, Dashboard, etc.)
3. Client.ServiceRegistration → Client services (OnlineClient, OfflineLicense)

### Phase 4: Testing (4 hours)
1. Build verification
2. API endpoint testing
3. Database migration testing

---

## Current Working Structure (Option A)

```
synflox-backend/
├── SYNFLOX.Microservices.sln
├── docker-compose.yml
│
├── core/                           ← ALL SHARED CODE
│   ├── SYNFLOX.Core.Domain/        
│   ├── SYNFLOX.Core.Application/   
│   ├── SYNFLOX.Core.Infrastructure/
│   └── SYNFLOX.Core.WebAPI/        
│
├── admin-api/                      ← Controllers only
│   └── src/SYNFLOX.Admin.WebAPI/   
│
└── client-api/                     ← Controllers only
    └── src/SYNFLOX.Client.WebAPI/  
```

**Build Status:** ✅ 0 Errors, ~113 Warnings

---

## Target Structure (Option B - NOT YET IMPLEMENTED)

```
synflox-backend/
├── core/                           ← ONLY truly shared
│   ├── Domain/                     
│   ├── Application/                (Base DTOs, Converters)
│   ├── Infrastructure/             (DbContext, BaseRepo)
│   └── WebAPI/                     (Common middlewares)
│
├── admin-api/                      ← FULL STACK
│   ├── Application/                (Admin DTOs, Interfaces)
│   ├── Infrastructure/             (Admin Services)
│   └── WebAPI/                     (19 Controllers)
│
└── client-api/                     ← FULL STACK
    ├── Application/                (Client DTOs, Interfaces)
    ├── Infrastructure/             (Client Services)
    └── WebAPI/                     (5 Controllers)
```

---

## Recommendation

**For now: Use Option A (current working state)**

Option B requires significant refactoring that should be done as a dedicated sprint, not as a quick task. The codebase works correctly with Option A and both APIs can be deployed and scaled independently at the infrastructure level (Docker/Kubernetes).

**When to revisit Option B:**
- When teams need to work independently on Admin vs Client
- When you need different release cycles for each API
- When the codebase grows and coupling becomes a maintenance issue
