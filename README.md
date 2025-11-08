# SYNFLOX - Central Licensing System
## Complete Enterprise Documentation

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [What is SYNFLOX?](#what-is-synflox)
3. [System Architecture](#system-architecture)
4. [Core Business Concepts](#core-business-concepts)
5. [Complete Feature List](#complete-feature-list)
6. [Business Workflows](#business-workflows)
7. [API Documentation](#api-documentation)
8. [Configuration Guide](#configuration-guide)
9. [Security Features](#security-features)
10. [Integration Guide](#integration-guide)
11. [Administration Guide](#administration-guide)
12. [Technical Details](#technical-details)
13. [Database Schema](#database-schema)
14. [Deployment Guide](#deployment-guide)

---

## Overview

**SYNFLOX** is a comprehensive **Central Licensing System** designed to manage and control licensing for multiple external enterprise products including ERP, CRM, POS, HR, Inventory systems, and more.

**Version**: 1.0  
**Status**: Production Ready (95% Complete)  
**Last Updated**: 2025-01-15

### Key Statistics

- **24 Controllers** with 200+ API endpoints
- **35+ Entities** in the domain model
- **50+ DTOs** for API communication
- **18 Major Features** fully implemented
- **Multi-language Support**: English and Arabic
- **Database Support**: SQL Server and Oracle
- **Architecture**: Clean Architecture with 4 layers

---

## What is SYNFLOX?

SYNFLOX is a **centralized licensing control system** that acts as the single source of truth for subscription management across multiple enterprise products. It provides:

### Primary Functions

1. **Company Management**: Create and manage companies (tenants) that represent customers
2. **Subscription Control**: Activate, suspend, resume, and extend subscriptions
3. **License Validation**: Real-time license status checking for external systems
4. **Trial Management**: Start and convert trial subscriptions
5. **Plan Management**: Define subscription plans with features and pricing
6. **Module System**: Flexible project-module-plan assignment system
7. **Analytics**: Track API usage, company activity, and system metrics
8. **Notifications**: Email and in-system notifications for expiry warnings
9. **Webhooks**: Event-driven notifications to external systems
10. **Reporting**: Comprehensive reports for business intelligence

### Use Cases

**Scenario 1: ERP System Licensing**
- Company purchases ERP system license
- SYNFLOX admin creates company record
- Admin activates subscription with expiry date
- ERP system calls SYNFLOX API to check status
- ERP allows/denies access based on SYNFLOX response

**Scenario 2: Multi-Product Licensing**
- Company has ERP, CRM, and POS systems
- All systems check same SYNFLOX company record
- Admin can suspend all systems by suspending company
- Admin can extend subscription for all systems at once

**Scenario 3: Trial to Paid Conversion**
- Admin starts 30-day trial for new company
- Company uses system during trial period
- Before trial expires, admin converts to paid subscription
- System continues without interruption

---

## System Architecture

SYNFLOX follows **Clean Architecture** principles with strict separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    WebAPI Layer                              │
│  Controllers, Middleware, Configuration, Startup            │
│  - 24 Controllers (200+ endpoints)                          │
│  - 9 Middleware components                                  │
│  - Authentication/Authorization                        │
│  - Localization (EN/AR)                                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                 Application Layer                            │
│  Business Logic Interfaces, DTOs, Mappings                 │
│  - 50+ DTOs (Request/Response)                              │
│  - Service Interfaces (Business Contracts)                  │
│  - AutoMapper Profiles                                      │
│  - Validation Logic                                         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                              │
│  Core Business Entities, Interfaces, Enums                  │
│  - 35+ Entities                                             │
│  - Repository Interfaces                                    │
│  - Business Enums                                           │
│  - Custom Exceptions                                        │
│  - NO dependencies on other layers                          │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              Infrastructure Layer                           │
│  Data Access, External Services, Implementations            │
│  - EF Core Context & Configurations                        │
│  - Repository Implementations                               │
│  - Service Implementations                                  │
│  - Background Workers                                       │
│  - Middleware Implementations                               │
│  - Authentication Services                                  │
└─────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### Domain Layer
- **Purpose**: Core business logic and entities
- **Dependencies**: None (pure business logic)
- **Contains**:
  - Entities (Company, SubscriptionPlan, Project, Module, etc.)
  - Enums (LicenseStatus, SubscriptionHistoryActionType, etc.)
  - Repository Interfaces
  - Custom Exceptions
  - Value Objects

#### Application Layer
- **Purpose**: Business logic contracts and data transfer
- **Dependencies**: Domain only
- **Contains**:
  - Service Interfaces (ILicensingService, ICompanyService, etc.)
  - DTOs (Data Transfer Objects)
  - AutoMapper Profiles
  - Validation Attributes

#### Infrastructure Layer
- **Purpose**: External concerns and implementations
- **Dependencies**: Domain and Application
- **Contains**:
  - EF Core Context and Configurations
  - Repository Implementations
  - Service Implementations
  - Background Workers
  - Email Services
  - File Services
  - Authentication Services

#### WebAPI Layer
- **Purpose**: HTTP interface and presentation
- **Dependencies**: All layers
- **Contains**:
  - Controllers
  - Middleware
  - Configuration
  - Startup Logic

---

## Core Business Concepts

### 1. Company (Tenant)

A **Company** represents a customer organization that uses one or more external products (ERP, CRM, POS, etc.).

**Key Properties**:
- `Id`: Unique identifier (encrypted GUID)
- `Name`: Company name
- `IsActive`: Subscription activation status
- `ExpiryDate`: Subscription expiration date
- `IsExpired`: Flag set by background worker
- `IsTrial`: Whether subscription is in trial period
- `TrialEndDate`: Trial expiration date
- `SubscriptionPlanId`: Associated subscription plan
- `LicenseKey`: Encrypted license key for offline systems
- `ContactEmail`, `ContactPhone`, `Address`: Contact information

**Business Rules**:
- Companies are created with `IsActive = false` by default
- Must be activated before external systems can access
- Status is calculated based on `IsActive`, `ExpiryDate`, and `IsExpired` flags
- Soft delete: Companies are marked as deleted, not permanently removed

### 2. License Status

SYNFLOX uses a **priority-based status calculation**:

1. **Expired** (Highest Priority)
   - If `IsExpired = true` OR `ExpiryDate < DateTime.UtcNow`
   - External systems are blocked
   - Background worker sets `IsExpired = true` automatically

2. **Suspended**
   - If `IsActive = false` AND `ExpiryDate >= DateTime.UtcNow`
   - Admin manually suspended subscription
   - External systems are blocked

3. **Active**
   - If `IsActive = true` AND `ExpiryDate >= DateTime.UtcNow` AND `IsExpired = false`
   - External systems can access normally

4. **Trial**
   - If `IsTrial = true` AND `TrialEndDate >= DateTime.UtcNow`
   - Special status for trial subscriptions
   - Can be converted to Active subscription

### 3. Subscription Plan

A **Subscription Plan** defines pricing, billing cycle, and features available to companies.

**Key Properties**:
- `Id`: Unique identifier
- `Name`: Plan name (e.g., "Basic", "Professional", "Enterprise")
- `Description`: Plan description
- `Price`: Monthly/Yearly price
- `Currency`: Currency code (USD, EUR, etc.)
- `BillingCycle`: Monthly or Yearly
- `IsActive`: Whether plan is available for assignment
- `Features`: JSON array of feature descriptions

**Business Rules**:
- Plans can be assigned to companies
- Plans can have multiple project-module combinations
- Plans define what features/modules a company can access

### 4. Project and Module System

SYNFLOX uses a flexible **Project → Module → Plan** system:

**Project**: Represents a product category (e.g., "ERP", "CRM", "POS")
- Contains multiple modules
- Can be associated with multiple plans

**Module**: Represents a feature within a project (e.g., "Inventory Management", "Sales", "Reports")
- Belongs to one or more projects
- Can be enabled/disabled per plan

**PlanProjectModule**: Junction table linking plans to specific project-module combinations
- Defines which modules are available in which plans
- Allows fine-grained feature control

**Example**:
```
Project: "ERP"
  ├── Module: "Inventory Management"
  ├── Module: "Sales"
  └── Module: "Reports"

Plan: "Professional"
  ├── ERP → Inventory Management (enabled)
  ├── ERP → Sales (enabled)
  └── ERP → Reports (disabled)
```

### 5. Subscription History (Audit Log)

Every subscription-related action is logged for audit purposes.

**Action Types**:
- `Created`: Company created
- `Activated`: Subscription activated
- `Suspended`: Subscription suspended
- `Resumed`: Subscription resumed
- `Extended`: Subscription extended
- `Expired`: Subscription expired (automatic)
- `Deleted`: Company deleted
- `TrialStarted`: Trial period started
- `TrialConverted`: Trial converted to paid

**Properties**:
- `CompanyId`: Company affected
- `ActionType`: Type of action
- `OldValue`: JSON snapshot of previous state
- `NewValue`: JSON snapshot of new state
- `PerformedBy`: Admin who performed action
- `Timestamp`: When action occurred
- `Notes`: Optional notes

### 6. Notifications

SYNFLOX sends notifications for important events:

**Notification Types**:
- `ExpiryWarning`: Subscription expiring soon (7, 3, 1 days)
- `Expired`: Subscription has expired
- `Suspended`: Subscription suspended
- `Activated`: Subscription activated
- `Resumed`: Subscription resumed
- `TrialStarted`: Trial period started
- `TrialExpiring`: Trial expiring soon

**Delivery Methods**:
1. **Email**: Sent to `Company.ContactEmail` if available
2. **In-System**: Stored in database for admin panel display

### 7. API Keys

API Keys allow external systems to authenticate without JWT tokens.

**Key Properties**:
- `CompanyId`: Associated company
- `KeyHash`: Hashed key (never stored in plain text)
- `KeyPrefix`: First 8 characters for display
- `Name`: Descriptive name
- `IsActive`: Whether key is active
- `ExpiresAt`: Optional expiration date
- `AllowedIps`: IP whitelist (CIDR notation supported)
- `RateLimitPerHour`: Custom rate limit

**Security Features**:
- Keys are hashed with SHA256 before storage
- Only key prefix is shown after creation
- IP whitelisting support
- Per-key rate limiting
- HMAC signing support for request signing

### 8. Webhooks

Webhooks allow SYNFLOX to notify external systems of events.

**Webhook Events**:
- `CompanyActivated`
- `CompanySuspended`
- `CompanyResumed`
- `CompanyExpired`
- `CompanyExtended`
- `TrialStarted`
- `TrialExpired`

**Features**:
- HMAC SHA256 signature for security
- Retry logic (3 attempts with exponential backoff)
- Delivery history tracking
- Configurable event subscriptions

---

## Complete Feature List

### Phase 1: Critical Missing Features ✅ COMPLETED

#### 1.1 Subscription History/Audit Log
- **Purpose**: Track all subscription-related changes
- **Entities**: `SubscriptionHistory`
- **Endpoints**: 
  - `GET /api/licensing/{id}/history` - Company history
  - `GET /api/licensing/history` - All history with filters
- **Features**:
  - Automatic logging of all actions
  - JSON snapshots of old/new values
  - Admin tracking
  - Date range filtering

#### 1.2 Expiry Notifications
- **Purpose**: Notify companies of upcoming/actual expiry
- **Entities**: `Notification`
- **Endpoints**:
  - `GET /api/notifications` - List notifications
  - `GET /api/notifications/company/{companyId}` - Company notifications
  - `PUT /api/notifications/{id}/read` - Mark as read
- **Features**:
  - Email notifications (7, 3, 1 days before expiry)
  - In-system notifications
  - Background worker for automatic sending
  - Localized messages (EN/AR)

#### 1.3 Bulk Operations
- **Purpose**: Perform actions on multiple companies simultaneously
- **Endpoints**:
  - `POST /api/licensing/bulk-activate`
  - `POST /api/licensing/bulk-suspend`
  - `POST /api/licensing/bulk-resume`
  - `POST /api/licensing/bulk-extend`
  - `POST /api/companies/bulk-delete`
  - `POST /api/companies/bulk-update`
- **Features**:
  - Process multiple companies in one request
  - Partial success handling
  - Detailed response with success/failure counts

#### 1.4 Company Usage Analytics
- **Purpose**: Track API usage per company
- **Entities**: `CompanyUsageLog`
- **Endpoints**:
  - `GET /api/analytics/company/{companyId}/usage`
  - `GET /api/analytics/api-usage`
  - `GET /api/analytics/api-usage/by-endpoint`
  - `GET /api/analytics/api-usage/by-company`
- **Features**:
  - Automatic logging via middleware
  - Request count, response time tracking
  - Endpoint-level analytics
  - Time series data

#### 1.5 API Key Authentication
- **Purpose**: Allow external systems to authenticate without JWT
- **Entities**: `ApiKey`
- **Endpoints**:
  - `POST /api/api-keys` - Create API key
  - `GET /api/api-keys` - List keys
  - `PUT /api/api-keys/{id}` - Update key
  - `DELETE /api/api-keys/{id}` - Revoke key
  - `PUT /api/api-keys/{id}/regenerate` - Regenerate key
- **Features**:
  - Secure key generation
  - IP whitelisting
  - Per-key rate limiting
  - HMAC signing support

#### 1.6 Background Service for Expiry
- **Purpose**: Automatically mark expired subscriptions
- **Service**: `SubscriptionExpiryWorker`
- **Features**:
  - Runs daily at configurable time (default: midnight)
  - Sets `IsExpired = true` for expired companies
  - Creates notifications
  - Triggers webhooks
  - Logs to subscription history

#### 1.7 Webhook Support
- **Purpose**: Event-driven notifications to external systems
- **Entities**: `Webhook`, `WebhookDelivery`
- **Endpoints**:
  - `POST /api/webhooks` - Create webhook
  - `GET /api/webhooks` - List webhooks
  - `GET /api/webhooks/{id}/deliveries` - Delivery history
  - `POST /api/webhooks/retry-failed` - Retry failed deliveries
- **Features**:
  - HMAC SHA256 signing
  - Retry logic (3 attempts)
  - Delivery tracking
  - Event filtering

#### 1.8 Subscription Plans/Tiers
- **Purpose**: Define subscription tiers with pricing
- **Entities**: `SubscriptionPlan`
- **Endpoints**:
  - `POST /api/subscription-plans` - Create plan
  - `GET /api/subscription-plans` - List plans
  - `PUT /api/subscription-plans/{id}` - Update plan
  - `DELETE /api/subscription-plans/{id}` - Delete plan
- **Features**:
  - Pricing and billing cycle
  - Feature descriptions
  - Plan assignment to companies

#### 1.9 Trial Periods
- **Purpose**: Support trial subscriptions
- **Endpoints**:
  - `POST /api/licensing/{id}/trial/start` - Start trial
  - `POST /api/licensing/{id}/trial/convert` - Convert to paid
- **Features**:
  - Configurable trial duration
  - Automatic trial expiry handling
  - Trial to paid conversion

#### 1.10 Modules/Features System
- **Purpose**: Flexible project-module-plan system
- **Entities**: `Project`, `Module`, `ProjectModule`, `PlanProjectModule`
- **Endpoints**:
  - Project CRUD: `/api/projects`
  - Module CRUD: `/api/modules`
  - Project-Module: `/api/project-modules`
  - Plan Modules: `/api/subscription-plans/{id}/project-modules`
- **Features**:
  - Many-to-many relationships
  - Plan-based module assignment
  - Company module access based on plan

### Phase 2: Security Enhancements ✅ COMPLETED

#### 2.1 Password Policy Enforcement
- **Purpose**: Enforce strong password requirements
- **Entities**: `PasswordPolicy`, `PasswordHistory`
- **Endpoints**:
  - `GET /api/password-policy` - Get policy
  - `PUT /api/password-policy` - Update policy
  - `POST /api/password-policy/validate` - Validate password
- **Features**:
  - NIST 800-63B compliant
  - Configurable complexity requirements
  - Password history tracking
  - Password expiration

#### 2.2 Request Signing (HMAC)
- **Purpose**: Prevent request tampering
- **Middleware**: `HmacSignatureMiddleware`
- **Features**:
  - HMAC SHA256 signing
  - Timestamp validation (5-minute window)
  - Replay attack prevention

#### 2.3 API Rate Limiting
- **Purpose**: Prevent API abuse
- **Middleware**: `RateLimitingMiddleware`
- **Features**:
  - Global rate limiting
  - Per-endpoint limits
  - Per-API-key limits
  - Sliding window algorithm
  - Rate limit headers in responses

#### 2.4 Failed Login Attempts Tracking
- **Purpose**: Prevent brute force attacks
- **Entities**: `LoginAttempt`
- **Endpoints**:
  - `GET /api/login-attempts` - View attempts
  - `GET /api/login-attempts/username/{username}/failed` - Failed attempts
- **Features**:
  - Automatic account lockout (5 failed attempts)
  - 30-minute lockout duration (configurable)
  - IP address tracking

#### 2.5 IP Whitelisting
- **Purpose**: Restrict API key access by IP
- **Features**:
  - CIDR notation support
  - Per-API-key IP whitelist
  - Automatic IP validation

### Phase 3: Monitoring & Observability ✅ COMPLETED

#### 3.1 Health Check Endpoint
- **Purpose**: Monitor system health
- **Endpoint**: `GET /api/health`
- **Features**:
  - Database connectivity check
  - Redis connectivity check (optional)
  - Disk space check
  - Email service check (optional)
  - Detailed health status

#### 3.2 Metrics/Telemetry
- **Purpose**: Track system metrics
- **Entities**: `SystemMetric`
- **Endpoints**:
  - `GET /api/metrics/summary` - Real-time metrics
  - `GET /api/metrics/history` - Historical metrics
  - `POST /api/metrics/aggregate` - Trigger aggregation
- **Features**:
  - Request count tracking
  - Response time tracking
  - Error rate tracking
  - Hourly/daily/monthly aggregation

#### 3.3 Performance Monitoring
- **Purpose**: Track slow queries and performance
- **Middleware**: `PerformanceMonitoringMiddleware`
- **Features**:
  - Request/response time tracking
  - Slow query detection (>1 second)
  - Automatic metric recording

#### 3.4 Error Tracking
- **Purpose**: Centralized error logging
- **Entities**: `ErrorLog`
- **Endpoints**:
  - `GET /api/errors` - List errors
  - `GET /api/errors/{errorId}` - Error details
  - `POST /api/errors/cleanup` - Cleanup old errors
- **Features**:
  - Structured error logging
  - Error ID tracking
  - Stack trace capture
  - Request context capture

#### 3.5 API Usage Analytics
- **Purpose**: Track overall API usage
- **Features**:
  - Usage by endpoint
  - Usage by company
  - Time series data
  - Response time analysis

### Phase 4: Business Features ✅ COMPLETED

#### 4.1 Company Groups/Categories
- **Purpose**: Group companies for bulk operations
- **Entities**: `CompanyGroup`, `CompanyGroupMember`
- **Endpoints**:
  - `POST /api/company-groups` - Create group
  - `GET /api/company-groups` - List groups
  - `POST /api/company-groups/{id}/companies` - Add companies
  - `POST /api/company-groups/{id}/bulk-activate` - Bulk activate group
- **Features**:
  - Group-based bulk operations
  - Company membership management
  - Group-level subscription control

#### 4.2 Custom Fields for Companies
- **Purpose**: Store additional company data
- **Entities**: `CompanyCustomField`
- **Endpoints**:
  - `GET /api/companies/{id}/custom-fields` - List fields
  - `POST /api/companies/{id}/custom-fields` - Create field
  - `PUT /api/companies/{id}/custom-fields/{fieldId}` - Update field
- **Features**:
  - Multiple field types (String, Number, Boolean, Date, JSON)
  - Per-company custom fields
  - Flexible data storage

#### 4.3 Export/Import
- **Purpose**: Bulk data operations
- **Endpoints**:
  - `GET /api/companies/export?format=csv|excel` - Export companies
  - `POST /api/companies/import` - Import companies
- **Features**:
  - CSV and Excel support
  - Validation and error reporting
  - Bulk import with error handling

#### 4.4 Reports & Dashboard
- **Purpose**: Business intelligence and reporting
- **Entities**: `ReportDefinition`
- **Endpoints**:
  - `GET /api/reports` - List available reports
  - `POST /api/reports/{reportType}/generate` - Generate report
  - `GET /api/reports/{reportType}/download` - Download report
- **Report Types**:
  - Subscription Expiry Report
  - Status Summary Report
  - Usage Analytics Report
  - Revenue Report
  - Trial Conversion Report
  - Module Usage Report
- **Dashboard**:
  - `GET /api/dashboard/statistics` - System statistics
  - `GET /api/dashboard/endpoints` - API endpoint discovery
  - `GET /api/dashboard/overview` - Complete overview

#### 4.5 Multi-Tenancy Support (Foundation)
- **Purpose**: Foundation for multi-tenant architecture
- **Entities**: `Tenant`
- **Endpoints**:
  - `POST /api/tenants` - Create tenant
  - `GET /api/tenants` - List tenants
- **Features**:
  - Tenant entity created
  - Tenant context middleware
  - Foundation for future expansion

### Additional Features ✅ COMPLETED

#### Admin Management
- **Purpose**: Manage system administrators
- **Entities**: `Admin`, `AdminType`
- **Endpoints**: `/api/admins`, `/api/admin-types`
- **Features**:
  - Admin CRUD operations
  - Role-based access control
  - Profile management
  - Password reset

#### Menu Items Management
- **Purpose**: Dynamic navigation menu
- **Entities**: `MenuItem`
- **Endpoints**: `/api/menuitems`
- **Features**:
  - Hierarchical menu structure
  - Role-based menu visibility
  - Dynamic menu loading

#### Search Functionality
- **Purpose**: Global search across entities
- **Endpoints**: `/api/search`
- **Features**:
  - Multi-entity search
  - Search suggestions
  - Entity type filtering

#### File Upload/Download
- **Purpose**: Chunked file transfer
- **Endpoints**: `/api/uploads`, `/api/downloads`
- **Features**:
  - Large file support
  - Chunked transfer
  - Progress tracking
  - Resume capability

---

## Business Workflows

### Workflow 1: New Company Onboarding

**Step 1: Create Company**
```
Admin → POST /api/companies
→ Company created with IsActive = false
→ SubscriptionHistory logged (Created)
```

**Step 2: Assign Subscription Plan (Optional)**
```
Admin → PUT /api/companies/{id}
→ Set SubscriptionPlanId
→ Company linked to plan
```

**Step 3: Start Trial (Optional)**
```
Admin → POST /api/licensing/{id}/trial/start
→ IsTrial = true
→ TrialEndDate = Now + trialDays
→ Notification created (TrialStarted)
→ Webhook triggered (TrialStarted)
→ SubscriptionHistory logged (TrialStarted)
```

**Step 4: Activate Subscription**
```
Admin → PUT /api/licensing/{id}/activate
→ IsActive = true
→ ExpiryDate set
→ Notification created (Activated)
→ Webhook triggered (CompanyActivated)
→ SubscriptionHistory logged (Activated)
```

**Step 5: Generate License Key (For Offline Systems)**
```
Admin → POST /api/licensing/{id}/license-key/generate
→ Encrypted license key generated
→ Stored in Company.LicenseKey
→ Key returned to admin
→ Admin installs key in external system
```

**Step 6: External System Validates**
```
External System → GET /api/licensing/{id}/status
→ Status: Active
→ External system allows access
```

### Workflow 2: Subscription Extension

**Step 1: Check Current Status**
```
Admin → GET /api/licensing/{id}/status
→ Current ExpiryDate retrieved
```

**Step 2: Extend Subscription**
```
Admin → PUT /api/licensing/{id}/extend
→ New ExpiryDate set
→ Notification created (if expiring soon)
→ Webhook triggered (CompanyExtended)
→ SubscriptionHistory logged (Extended)
```

**Step 3: External System Continues Access**
```
External System → GET /api/licensing/{id}/status
→ Status: Active (with new ExpiryDate)
→ Access continues
```

### Workflow 3: Subscription Suspension

**Step 1: Suspend Subscription**
```
Admin → PUT /api/licensing/{id}/suspend
→ IsActive = false
→ Notification created (Suspended)
→ Webhook triggered (CompanySuspended)
→ SubscriptionHistory logged (Suspended)
```

**Step 2: External System Checks Status**
```
External System → GET /api/licensing/{id}/status
→ Status: Suspended
→ External system blocks access
→ Shows localized message: "Subscription is suspended" / "الاشتراك معلق"
```

**Step 3: Resume Subscription**
```
Admin → PUT /api/licensing/{id}/resume
→ IsActive = true
→ Notification created (Resumed)
→ Webhook triggered (CompanyResumed)
→ SubscriptionHistory logged (Resumed)
```

### Workflow 4: Automatic Expiry

**Step 1: Background Worker Runs (Daily at Midnight)**
```
SubscriptionExpiryWorker → Check companies where ExpiryDate < Now
→ For each expired company:
  → Set IsExpired = true
  → Set IsActive = false
  → Create Notification (Expired)
  → Trigger Webhook (CompanyExpired)
  → Log to SubscriptionHistory (Expired)
```

**Step 2: External System Checks Status**
```
External System → GET /api/licensing/{id}/status
→ Status: Expired
→ External system blocks access
→ Shows localized message: "Subscription has expired" / "انتهت صلاحية الاشتراك"
```

**Step 3: Admin Extends Subscription**
```
Admin → PUT /api/licensing/{id}/extend
→ New ExpiryDate set
→ IsExpired = false (if new date > Now)
→ IsActive = true
→ Notification created (Activated)
→ Webhook triggered (CompanyActivated)
→ SubscriptionHistory logged (Extended)
```

### Workflow 5: Trial to Paid Conversion

**Step 1: Trial Period Active**
```
Company → IsTrial = true
→ TrialEndDate = Now + 30 days
→ External system has access
```

**Step 2: Admin Converts to Paid**
```
Admin → POST /api/licensing/{id}/trial/convert
→ IsTrial = false
→ IsActive = true
→ ExpiryDate set
→ Notification created (Activated)
→ Webhook triggered (CompanyActivated)
→ SubscriptionHistory logged (TrialConverted)
```

**Step 3: External System Continues Access**
```
External System → GET /api/licensing/{id}/status
→ Status: Active (no longer Trial)
→ Access continues seamlessly
```

### Workflow 6: Bulk Operations

**Step 1: Select Multiple Companies**
```
Admin → Select companies in admin panel
→ Company IDs collected
```

**Step 2: Perform Bulk Action**
```
Admin → POST /api/licensing/bulk-activate
→ Request: { CompanyIds: [...], ExpiryDate: "2025-12-31" }
→ For each company:
  → Activate subscription
  → Create notification
  → Trigger webhook
  → Log to history
→ Response: { totalRequested: 10, successful: 9, failed: 1 }
```

**Step 3: Review Results**
```
Admin → View bulk operation response
→ See which companies succeeded/failed
→ Take action on failures if needed
```

### Workflow 7: Webhook Delivery

**Step 1: Event Occurs**
```
LicensingService → Company activated
→ IWebhookService.TriggerWebhookAsync() called
```

**Step 2: Find Active Webhooks**
```
WebhookService → Query webhooks for company
→ Filter by event type (CompanyActivated)
→ Filter by IsActive = true
```

**Step 3: Deliver Webhook**
```
WebhookService → For each webhook:
  → Create HMAC signature
  → POST to webhook URL
  → Log delivery attempt
  → If failed: Retry (3 attempts with backoff)
```

**Step 4: External System Receives**
```
External System → Receives POST request
→ Validates HMAC signature
→ Processes event
→ Returns 200 OK
```

---

## API Documentation

### Authentication Endpoints

#### Admin Login
```http
POST /api/admin/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password123"
}
```

**Response**:
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 1800
}
```

#### Refresh Token
```http
POST /api/admin/auth/refresh-token
Authorization: Bearer {access_token}
```

#### Logout
```http
POST /api/admin/auth/logout
Authorization: Bearer {access_token}
```

### Company Management Endpoints

#### Create Company
```http
POST /api/companies
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "name": "Acme Corporation",
  "expiryDate": "2025-12-31T23:59:59Z",
  "contactEmail": "admin@acme.com",
  "contactPhone": "+1234567890",
  "address": "123 Main St, City, Country",
  "subscriptionPlanId": "encrypted_plan_id"
}
```

#### Get All Companies
```http
GET /api/companies?page=1&pageSize=10&search=Acme
Authorization: Bearer {admin_token}
```

#### Get Company by ID
```http
GET /api/companies/{encrypted_id}
Authorization: Bearer {admin_token}
```

#### Update Company
```http
PUT /api/companies/{encrypted_id}
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "name": "Acme Corporation Updated",
  "contactEmail": "newadmin@acme.com"
}
```

#### Delete Company
```http
DELETE /api/companies/{encrypted_id}
Authorization: Bearer {superadmin_token}
```

### Licensing Operations

#### Activate Company
```http
PUT /api/licensing/{encrypted_id}/activate
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

#### Suspend Company
```http
PUT /api/licensing/{encrypted_id}/suspend
Authorization: Bearer {superadmin_token}
```

#### Resume Company
```http
PUT /api/licensing/{encrypted_id}/resume
Authorization: Bearer {superadmin_token}
```

#### Extend Subscription
```http
PUT /api/licensing/{encrypted_id}/extend
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "newExpiryDate": "2026-12-31T23:59:59Z"
}
```

#### Check Company Status (Public)
```http
GET /api/licensing/{encrypted_id}/status
Accept-Language: en
```

**Response (Active)**:
```json
{
  "statusCode": 200,
  "message": "Subscription is active",
  "data": {
    "status": "Active",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "statusMessage": "Subscription is active"
  }
}
```

#### Generate License Key
```http
POST /api/licensing/{encrypted_id}/license-key/generate
Authorization: Bearer {superadmin_token}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "License key generated successfully",
  "data": {
    "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "message": "License key generated successfully"
  }
}
```

#### Validate License Key (Public)
```http
POST /api/licensing/validate-key
Content-Type: application/json

{
  "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Subscription Plans

#### Create Plan
```http
POST /api/subscription-plans
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "name": "Professional",
  "description": "Professional plan with all features",
  "price": 99.99,
  "currency": "USD",
  "billingCycle": "Monthly",
  "isActive": true,
  "features": ["Feature 1", "Feature 2"]
}
```

#### Get All Plans
```http
GET /api/subscription-plans?isActive=true&page=1&pageSize=10
Authorization: Bearer {superadmin_token}
```

#### Update Plan Project Modules
```http
PUT /api/subscription-plans/{encrypted_id}/project-modules
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "projectModuleIds": ["encrypted_id_1", "encrypted_id_2"]
}
```

### Projects and Modules

#### Create Project
```http
POST /api/projects
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "name": "ERP System",
  "description": "Enterprise Resource Planning"
}
```

#### Create Module
```http
POST /api/modules
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "name": "Inventory Management",
  "description": "Inventory tracking and management"
}
```

#### Create Project-Module Relationship
```http
POST /api/project-modules
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "projectId": "encrypted_project_id",
  "moduleId": "encrypted_module_id"
}
```

### Bulk Operations

#### Bulk Activate
```http
POST /api/licensing/bulk-activate
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "companyIds": ["encrypted_id_1", "encrypted_id_2"],
  "action": "Activate",
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "Bulk operation completed",
  "data": {
    "totalRequested": 2,
    "successful": 2,
    "failed": 0,
    "errorMessage": null
  }
}
```

### Notifications

#### Get Notifications
```http
GET /api/notifications?companyId={encrypted_id}&unreadOnly=true&page=1&pageSize=10
Authorization: Bearer {admin_token}
```

#### Mark Notification as Read
```http
PUT /api/notifications/{encrypted_id}/read
Authorization: Bearer {admin_token}
```

### API Keys

#### Create API Key
```http
POST /api/api-keys
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "companyId": "encrypted_company_id",
  "name": "ERP System API Key",
  "expiresAt": "2025-12-31T23:59:59Z",
  "allowedIps": ["192.168.1.0/24"],
  "rateLimitPerHour": 1000
}
```

**Response**:
```json
{
  "statusCode": 201,
  "message": "API key created successfully",
  "data": {
    "id": "encrypted_key_id",
    "key": "sk_live_abc123...",
    "keyPrefix": "sk_live_",
    "message": "API key created successfully. Store this key securely - it will not be shown again."
  }
}
```

#### Use API Key
```http
GET /api/licensing/{encrypted_id}/status
Authorization: Bearer sk_live_abc123...
```

### Webhooks

#### Create Webhook
```http
POST /api/webhooks
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "companyId": "encrypted_company_id",
  "url": "https://external-system.com/webhook",
  "secret": "webhook_secret",
  "events": ["CompanyActivated", "CompanySuspended", "CompanyExpired"],
  "isActive": true
}
```

#### Get Webhook Deliveries
```http
GET /api/webhooks/{encrypted_id}/deliveries?page=1&pageSize=10
Authorization: Bearer {superadmin_token}
```

### Analytics

#### Get Company Usage
```http
GET /api/analytics/company/{encrypted_id}/usage?fromDate=2025-01-01&toDate=2025-01-31
Authorization: Bearer {superadmin_token}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "",
  "data": {
    "companyId": "encrypted_id",
    "totalRequests": 15420,
    "requestsByEndpoint": {
      "/api/licensing/{id}/status": 12000,
      "/api/licensing/validate-key": 3420
    },
    "averageResponseTime": 45.2,
    "lastRequestTime": "2025-01-15T10:30:00Z",
    "requestsByDate": {
      "2025-01-01": 500,
      "2025-01-02": 520
    }
  }
}
```

### Reports

#### Generate Report
```http
POST /api/reports/SubscriptionExpiry/generate
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "days": 30
}
```

#### Download Report
```http
GET /api/reports/SubscriptionExpiry/download?format=excel
Authorization: Bearer {superadmin_token}
```

### Dashboard

#### Get System Statistics
```http
GET /api/dashboard/statistics
Authorization: Bearer {admin_token}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "Statistics retrieved successfully",
  "data": {
    "totalCompanies": 150,
    "activeCompanies": 120,
    "expiredCompanies": 20,
    "suspendedCompanies": 10,
    "trialCompanies": 5,
    "statusBreakdown": {
      "Active": 120,
      "Expired": 20,
      "Suspended": 10,
      "Trial": 5
    }
  }
}
```

### Search

#### Global Search
```http
POST /api/search
Content-Type: application/json

{
  "query": "Acme",
  "page": 1,
  "pageSize": 10,
  "entityTypes": ["Company", "Admin"],
  "includeInactive": false
}
```

---

## Configuration Guide

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SYNFLOX;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "Issuer": "SYNFLOX",
    "Audience": "SYNFLOX",
    "SecretKey": "your-secret-key-here-min-32-chars",
    "Lifetime": 30,
    "RefreshTokenExpiration": 60
  },
  "LicenseKeySettings": {
    "EncryptionKey": "base64-encoded-32-byte-key",
    "IV": "base64-encoded-16-byte-iv",
    "SigningKey": "base64-encoded-32-byte-key",
    "Version": 1
  },
  "RateLimiting": {
    "GlobalLimitPerHour": 1000,
    "EndpointLimits": {
      "/api/licensing/{id}/status": 100,
      "/api/licensing/validate-key": 50
    },
    "DefaultApiKeyLimitPerHour": 100
  },
  "NotificationSettings": {
    "ExpiryWarningDays": [7, 3, 1],
    "EmailEnabled": true,
    "InSystemEnabled": true
  },
  "ExpiryCheckSettings": {
    "CheckIntervalHours": 24,
    "CheckTime": "00:00"
  },
  "LoginSecuritySettings": {
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 30
  },
  "PasswordPolicy": {
    "MinLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireNumbers": true,
    "RequireSpecialChars": true,
    "MaxAgeDays": 90,
    "PreventReuseCount": 5
  },
  "FileUpload": {
    "MaxFileSize": 104857600,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx"],
    "ChunkSize": 1048576
  }
}
```

### Environment Variables

For production, use environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=prod-server;Database=SYNFLOX;...
JwtSettings__SecretKey=production-secret-key
```

---

## Security Features

### 1. JWT Authentication
- Secure token-based authentication
- Access tokens (30 minutes)
- Refresh tokens (60 days)
- Token revocation support

### 2. Role-Based Authorization
- **SuperAdmin**: Full system access
- **Admin**: Limited access (read-only for some features)
- Policy-based authorization (`SuperAdminOnly`, `AdminOrSuperAdmin`)

### 3. ID Encryption
- All GUIDs are encrypted in API responses
- Prevents ID enumeration attacks
- Automatic encryption/decryption in mappers

### 4. License Key Security
- AES-256 encryption
- HMAC SHA256 signing
- Clock tampering detection
- Version control

### 5. API Key Security
- SHA256 hashing (never stored in plain text)
- IP whitelisting (CIDR support)
- Per-key rate limiting
- HMAC request signing

### 6. Password Security
- NIST 800-63B compliant policies
- Password history tracking
- Password expiration
- Account lockout after failed attempts

### 7. Rate Limiting
- Global rate limiting
- Per-endpoint limits
- Per-API-key limits
- Sliding window algorithm

### 8. Request Signing
- HMAC SHA256 signatures
- Timestamp validation (5-minute window)
- Replay attack prevention

### 9. Soft Delete
- Companies are never permanently deleted
- `IsDeleted` flag for soft deletion
- Audit trail preserved

### 10. Audit Logging
- All subscription changes logged
- Admin action tracking
- JSON snapshots of old/new values

---

## Integration Guide

### Online System Integration

**Step 1: Get Company ID**
- Admin provides encrypted company ID
- Store in external system configuration

**Step 2: Check Status on Startup**
```csharp
public async Task<bool> ValidateLicense(Guid companyId)
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept-Language", "en");
    
    var response = await client.GetAsync(
        $"https://synflox-api.com/api/licensing/{companyId}/status");
    
    if (!response.IsSuccessStatusCode)
        return false;
    
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyStatusResponse>>();
    
    return result.Data.Status == "Active";
}
```

**Step 3: Periodic Status Checks**
- Check status every 5-10 minutes
- Cache result to reduce API calls
- Handle network errors gracefully

### Offline System Integration

**Step 1: Generate License Key**
- Admin generates key via API
- Key is encrypted and signed

**Step 2: Install Key**
- Store key in external system configuration
- Key contains all necessary information

**Step 3: Validate Locally**
```csharp
public bool ValidateOfflineLicense(string licenseKey)
{
    // Decrypt and validate locally
    // Check expiry date
    // Detect clock tampering
    // Return validation result
}
```

**Step 4: Online Validation (When Available)**
- Periodically validate key online
- Update local cache
- Detect tampering

### Webhook Integration

**Step 1: Register Webhook**
```http
POST /api/webhooks
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "companyId": "encrypted_company_id",
  "url": "https://your-system.com/webhook",
  "secret": "your_webhook_secret",
  "events": ["CompanyActivated", "CompanySuspended", "CompanyExpired"],
  "isActive": true
}
```

**Step 2: Receive Webhook**
```csharp
[HttpPost("/webhook")]
public async Task<IActionResult> ReceiveWebhook(
    [FromHeader(Name = "X-Webhook-Signature")] string signature,
    [FromBody] WebhookPayload payload)
{
    // Validate HMAC signature
    var expectedSignature = ComputeHMAC(payload, webhookSecret);
    if (signature != expectedSignature)
        return Unauthorized();
    
    // Process event
    switch (payload.EventType)
    {
        case "CompanyActivated":
            // Handle activation
            break;
        case "CompanySuspended":
            // Handle suspension
            break;
    }
    
    return Ok();
}
```

### API Key Integration

**Step 1: Create API Key**
- Admin creates API key for company
- Key is returned once (store securely)

**Step 2: Use API Key**
```http
GET /api/licensing/{company_id}/status
Authorization: Bearer {api_key}
```

**Step 3: Handle Rate Limits**
- Check `X-RateLimit-Remaining` header
- Respect rate limits
- Implement exponential backoff

---

## Administration Guide

### Creating Your First Company

1. **Login as SuperAdmin**
   ```http
   POST /api/admin/auth/login
   {
     "username": "superadmin",
     "password": "password"
   }
   ```

2. **Create Company**
   ```http
   POST /api/companies
   Authorization: Bearer {token}
   {
     "name": "Test Company",
     "contactEmail": "admin@test.com"
   }
   ```

3. **Activate Subscription**
   ```http
   PUT /api/licensing/{company_id}/activate
   Authorization: Bearer {token}
   {
     "expiryDate": "2025-12-31T23:59:59Z"
   }
   ```

4. **Generate License Key (If Needed)**
   ```http
   POST /api/licensing/{company_id}/license-key/generate
   Authorization: Bearer {token}
   ```

### Managing Subscription Plans

1. **Create Plan**
   ```http
   POST /api/subscription-plans
   {
     "name": "Basic",
     "price": 49.99,
     "billingCycle": "Monthly"
   }
   ```

2. **Create Projects and Modules**
   ```http
   POST /api/projects
   { "name": "ERP System" }
   
   POST /api/modules
   { "name": "Inventory Management" }
   ```

3. **Link Project to Module**
   ```http
   POST /api/project-modules
   {
     "projectId": "...",
     "moduleId": "..."
   }
   ```

4. **Assign Modules to Plan**
   ```http
   PUT /api/subscription-plans/{plan_id}/project-modules
   {
     "projectModuleIds": ["...", "..."]
   }
   ```

5. **Assign Plan to Company**
   ```http
   PUT /api/companies/{company_id}
   {
     "subscriptionPlanId": "..."
   }
   ```

### Bulk Operations

**Bulk Activate Multiple Companies**:
```http
POST /api/licensing/bulk-activate
{
  "companyIds": ["id1", "id2", "id3"],
  "action": "Activate",
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

**Bulk Delete Companies**:
```http
POST /api/companies/bulk-delete
{
  "companyIds": ["id1", "id2"],
  "action": "Delete"
}
```

### Monitoring System Health

**Check Health**:
```http
GET /api/health
```

**View Metrics**:
```http
GET /api/metrics/summary
```

**View Error Logs**:
```http
GET /api/errors?fromDate=2025-01-01&toDate=2025-01-31
```

---

## Technical Details

### ID Encryption

All GUIDs are encrypted using AES-256 before being sent to clients:

**Encryption Process**:
1. Generate random IV
2. Encrypt GUID with AES-256
3. Base64 encode result
4. Return encrypted string

**Decryption Process**:
1. Base64 decode string
2. Decrypt with AES-256
3. Return original GUID

**Implementation**: `IIdEncryptionService` with `EncryptGuidConverter` and `DecryptGuidConverter` for AutoMapper

### License Key Format

License keys are encrypted JWT-like tokens:

```
License Key = Base64(AES-256_Encrypt(JSON({
    Payload: {
        CompanyId: Guid,
        ExpiryDate: DateTime,
        IssuedDate: DateTime,
        Version: int
    },
    Signature: HMAC_SHA256(Payload, SigningKey)
})))
```

**Validation Steps**:
1. Base64 decode
2. AES-256 decrypt
3. Verify HMAC signature
4. Check expiry date
5. Detect clock tampering (current time >= issued time)

### Background Workers

#### SubscriptionExpiryWorker
- **Schedule**: Daily at configurable time (default: 00:00)
- **Purpose**: Mark expired subscriptions
- **Actions**:
  - Find companies where `ExpiryDate < Now` AND `IsExpired = false`
  - Set `IsExpired = true`
  - Set `IsActive = false`
  - Create notification
  - Trigger webhook
  - Log to subscription history

#### ExpiryNotificationWorker
- **Schedule**: Daily at configurable time (default: 09:00)
- **Purpose**: Send expiry warnings
- **Actions**:
  - Check companies expiring in 7, 3, 1 days
  - Send email notifications
  - Create in-system notifications

### Middleware Pipeline

Request processing order:

1. **ExceptionHandlingMiddleware** - Global error handling
2. **ApiKeyAuthenticationMiddleware** - API key validation
3. **HmacSignatureMiddleware** - Request signature validation
4. **RateLimitingMiddleware** - Rate limit enforcement
5. **UsageTrackingMiddleware** - API usage logging
6. **PerformanceMonitoringMiddleware** - Performance tracking
7. **Authentication** - JWT validation
8. **CustomClaimsPrincipalMiddleware** - Claims transformation
9. **Authorization** - Policy enforcement
10. **CachingMiddleware** - Response caching
11. **LoggingMiddleware** - Request logging
12. **Controller** - Business logic

### Database Migrations

EF Core migrations are used for database schema management:

```bash
# Create migration
dotnet ef migrations add AddNewFeature --project Infrastructure --startup-project WebAPI

# Update database
dotnet ef database update --project Infrastructure --startup-project WebAPI
```

### Localization

SYNFLOX supports English and Arabic:

**Language Detection** (Priority Order):
1. `Accept-Language` header (RFC 7231)
2. `X-Language` custom header
3. `?lang=` query parameter
4. Default: English

**Resource Files**:
- `Infrastructure/Resources/SharedResource.resx` (English)
- `Infrastructure/Resources/SharedResource.ar.resx` (Arabic)

**Usage**:
```csharp
var message = _localizer["Company.CompanyCreated"];
// Returns: "Company created successfully" (EN) or "تم إنشاء الشركة بنجاح" (AR)
```

---

## Database Schema

### Core Tables

#### Companies
- `Id` (Guid, PK)
- `Name` (string)
- `IsActive` (bool)
- `ExpiryDate` (DateTime?)
- `IsExpired` (bool)
- `IsTrial` (bool)
- `TrialEndDate` (DateTime?)
- `SubscriptionPlanId` (Guid?, FK)
- `LicenseKey` (string?)
- `ContactEmail`, `ContactPhone`, `Address` (string?)
- `CreatedTimestamp`, `UpdatedTimestamp`, `DeletedTimestamp` (DateTime?)
- `IsDeleted` (bool)

#### SubscriptionPlans
- `Id` (Guid, PK)
- `Name`, `Description` (string)
- `Price` (decimal)
- `Currency` (string)
- `BillingCycle` (enum)
- `IsActive` (bool)
- `Features` (JSON)

#### Projects
- `Id` (Guid, PK)
- `Name`, `Description` (string)
- `IsActive` (bool)

#### Modules
- `Id` (Guid, PK)
- `Name`, `Description` (string)
- `IsActive` (bool)

#### ProjectModules (Junction)
- `ProjectId` (Guid, FK)
- `ModuleId` (Guid, FK)
- Composite PK

#### PlanProjectModules (Junction)
- `SubscriptionPlanId` (Guid, FK)
- `ProjectModuleId` (Guid, FK)
- Composite PK

#### SubscriptionHistory
- `Id` (Guid, PK)
- `CompanyId` (Guid, FK)
- `ActionType` (enum)
- `OldValue`, `NewValue` (JSON)
- `PerformedBy` (Guid, FK to Admin)
- `Timestamp` (DateTime)
- `Notes` (string?)

#### Notifications
- `Id` (Guid, PK)
- `CompanyId` (Guid, FK)
- `Type` (enum)
- `Title`, `Message` (string)
- `IsRead` (bool)
- `ReadAt` (DateTime?)
- `CreatedAt` (DateTime)

#### ApiKeys
- `Id` (Guid, PK)
- `CompanyId` (Guid, FK)
- `KeyHash` (string)
- `KeyPrefix` (string)
- `Name` (string)
- `IsActive` (bool)
- `ExpiresAt` (DateTime?)
- `AllowedIps` (JSON)
- `RateLimitPerHour` (int?)

#### Webhooks
- `Id` (Guid, PK)
- `CompanyId` (Guid, FK)
- `Url` (string)
- `Secret` (string)
- `Events` (JSON)
- `IsActive` (bool)
- `RetryCount` (int)

#### WebhookDeliveries
- `Id` (Guid, PK)
- `WebhookId` (Guid, FK)
- `EventType` (enum)
- `Payload` (JSON)
- `StatusCode` (int?)
- `ResponseBody` (string?)
- `AttemptedAt` (DateTime)
- `Succeeded` (bool)

### Indexes

Key indexes for performance:
- `Companies.Name` (non-unique)
- `Companies.LicenseKey` (unique, filtered: `IsDeleted = false`)
- `Companies.ExpiryDate` (non-unique)
- `Companies.IsActive` (non-unique)
- `Companies.IsExpired` (non-unique)
- `SubscriptionHistory.CompanyId` (non-unique)
- `SubscriptionHistory.Timestamp` (non-unique)
- `ApiKeys.KeyHash` (unique)
- `ApiKeys.CompanyId` (non-unique)

---

## Deployment Guide

### Prerequisites

- .NET 8.0 SDK
- SQL Server 2019+ or Oracle 12c+
- Redis (optional, for distributed caching)
- SMTP server (for email notifications)

### Production Deployment Steps

1. **Configure Database**
   - Update connection string
   - Run migrations: `dotnet ef database update`

2. **Configure Settings**
   - Update `appsettings.Production.json`
   - Set secure JWT secret key
   - Configure license key encryption keys
   - Set up email SMTP settings

3. **Configure Environment Variables**
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=...
   JwtSettings__SecretKey=...
   ```

4. **Build Application**
   ```bash
   dotnet build --configuration Release
   ```

5. **Publish Application**
   ```bash
   dotnet publish --configuration Release --output ./publish
   ```

6. **Deploy to Server**
   - Copy publish folder to server
   - Configure IIS or Kestrel
   - Set up reverse proxy (nginx/Apache)

7. **Create SuperAdmin**
   - Use database initializer
   - Or create via SQL script

8. **Verify Deployment**
   - Check health endpoint: `GET /api/health`
   - Test login: `POST /api/admin/auth/login`
   - Verify Swagger: `GET /swagger`

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

```bash
docker build -t synflox-api .
docker run -d -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="..." \
  synflox-api
```

---

## Support and Resources

### API Documentation
- **Swagger UI**: `/swagger` (when running)
- **OpenAPI JSON**: `/swagger/v1/swagger.json`

### Health Monitoring
- **Health Check**: `GET /api/health`
- **Metrics**: `GET /api/metrics/summary`
- **Error Logs**: `GET /api/errors`

### Getting Help
- Review Swagger documentation
- Check error logs via API
- Review subscription history for audit trail
- Check webhook delivery history for integration issues

---

**SYNFLOX** - Central Licensing System for Enterprise Products  
**Version**: 1.0  
**Status**: Production Ready (95% Complete)  
**Last Updated**: 2025-01-15

---

## Complete Enum Reference

### LicenseStatus
```csharp
public enum LicenseStatus
{
    Active = 1,      // Company is active and subscription is valid
    Expired = 2,     // Subscription has expired
    Suspended = 3    // Company subscription is suspended (manually deactivated)
}
```

**Business Logic**:
- `Active`: Company can use external systems normally
- `Expired`: Company cannot use external systems (automatic or manual expiry)
- `Suspended`: Company cannot use external systems (admin manually suspended)

### SubscriptionHistoryActionType
```csharp
public enum SubscriptionHistoryActionType
{
    Created = 1,           // Company created
    Activated = 2,         // Subscription activated
    Suspended = 3,         // Subscription suspended
    Resumed = 4,           // Subscription resumed
    Extended = 5,          // Subscription extended
    Expired = 6,           // Subscription expired (automatic)
    Deleted = 7,           // Company deleted
    TrialStarted = 8,      // Trial period started
    TrialConverted = 9,    // Trial converted to paid
    TrialExpired = 10      // Trial period expired
}
```

**When Each Action is Logged**:
- `Created`: When company is first created via `POST /api/companies`
- `Activated`: When subscription is activated via `PUT /api/licensing/{id}/activate`
- `Suspended`: When subscription is suspended via `PUT /api/licensing/{id}/suspend`
- `Resumed`: When subscription is resumed via `PUT /api/licensing/{id}/resume`
- `Extended`: When subscription is extended via `PUT /api/licensing/{id}/extend`
- `Expired`: Automatically by `SubscriptionExpiryWorker` when `ExpiryDate < Now`
- `Deleted`: When company is soft-deleted via `DELETE /api/companies/{id}`
- `TrialStarted`: When trial is started via `POST /api/licensing/{id}/trial/start`
- `TrialConverted`: When trial is converted via `POST /api/licensing/{id}/trial/convert`
- `TrialExpired`: Automatically when `TrialEndDate < Now`

### NotificationType
```csharp
public enum NotificationType
{
    ExpiryWarning = 1,  // Warning that subscription is expiring soon
    Expired = 2,        // Subscription has expired
    Suspended = 3,      // Subscription was suspended
    Activated = 4,      // Subscription was activated
    Resumed = 5,        // Subscription was resumed
    Extended = 6,       // Subscription expiry date was extended
    General = 7         // General notification
}
```

**When Notifications are Created**:
- `ExpiryWarning`: By `ExpiryNotificationWorker` (7, 3, 1 days before expiry)
- `Expired`: By `SubscriptionExpiryWorker` when subscription expires
- `Suspended`: When admin suspends subscription
- `Activated`: When admin activates subscription
- `Resumed`: When admin resumes subscription
- `Extended`: When admin extends subscription
- `General`: For custom notifications

### WebhookEventType
```csharp
public enum WebhookEventType
{
    CompanyActivated = 1,
    CompanySuspended = 2,
    CompanyResumed = 3,
    CompanyExpired = 4,
    CompanyExtended = 5,
    TrialStarted = 6,
    TrialExpired = 7,
    TrialConverted = 8
}
```

**When Webhooks are Triggered**:
- `CompanyActivated`: After successful activation
- `CompanySuspended`: After successful suspension
- `CompanyResumed`: After successful resume
- `CompanyExpired`: When `SubscriptionExpiryWorker` marks company as expired
- `CompanyExtended`: After successful extension
- `TrialStarted`: After trial is started
- `TrialExpired`: When trial period expires
- `TrialConverted`: After trial is converted to paid

### BillingCycle
```csharp
public enum BillingCycle
{
    Monthly = 1,   // Monthly billing
    Yearly = 2     // Yearly billing
}
```

**Usage**: Defines how often subscription plan is billed

### CustomFieldType
```csharp
public enum CustomFieldType
{
    String = 1,   // Text field
    Number = 2,   // Numeric field
    Boolean = 3,  // True/False field
    Date = 4,     // Date field
    Json = 5      // Complex JSON data
}
```

**Usage**: Defines the data type for company custom fields

### ReportType
```csharp
public enum ReportType
{
    SubscriptionExpiry = 1,  // Companies expiring in X days
    StatusSummary = 2,       // Count by status (Active/Expired/Suspended)
    UsageAnalytics = 3,      // API usage statistics
    Revenue = 4,            // Revenue by plan/company
    TrialConversion = 5,    // Trial to paid conversion rates
    ModuleUsage = 6,        // Module usage statistics
    Custom = 99             // Custom report (future)
}
```

### MetricType
```csharp
public enum MetricType
{
    RequestCount = 1,        // Total API requests
    ResponseTime = 2,        // Average response time (ms)
    ErrorRate = 3,          // Error rate percentage
    ActiveCompanies = 4,     // Number of active companies
    ExpiredCompanies = 5,   // Number of expired companies
    SuspendedCompanies = 6, // Number of suspended companies
    ApiKeyCount = 7,        // Total API keys
    DatabaseQueryTime = 8,  // Database query time (ms)
    MemoryUsage = 9,        // Memory usage (MB)
    CpuUsage = 10           // CPU usage percentage
}
```

### BulkOperationAction
```csharp
public enum BulkOperationAction
{
    Activate = 1,
    Suspend = 2,
    Resume = 3,
    Extend = 4,
    Delete = 5,
    Update = 6
}
```

---

## Detailed Business Logic Explanations

### Status Calculation Algorithm

SYNFLOX uses a **priority-based status calculation** that ensures consistent and predictable results:

```csharp
public LicenseStatus CalculateStatus(Company company)
{
    var now = DateTime.UtcNow;
    
    // Priority 1: Expired (Highest Priority)
    if (company.IsExpired || 
        (company.ExpiryDate.HasValue && company.ExpiryDate.Value < now))
    {
        return LicenseStatus.Expired;
    }
    
    // Priority 2: Trial Expired
    if (company.IsTrial && 
        company.TrialEndDate.HasValue && 
        company.TrialEndDate.Value < now)
    {
        return LicenseStatus.Expired;
    }
    
    // Priority 3: Suspended
    if (!company.IsActive && 
        (!company.ExpiryDate.HasValue || company.ExpiryDate.Value >= now))
    {
        return LicenseStatus.Suspended;
    }
    
    // Priority 4: Active
    if (company.IsActive && 
        (!company.ExpiryDate.HasValue || company.ExpiryDate.Value >= now))
    {
        return LicenseStatus.Active;
    }
    
    // Default: Expired (safety fallback)
    return LicenseStatus.Expired;
}
```

**Why This Order Matters**:
1. **Expired First**: Even if `IsActive = true`, if expired, status is Expired
2. **Suspended Second**: If not expired but `IsActive = false`, status is Suspended
3. **Active Last**: Only if not expired and `IsActive = true`, status is Active

### License Key Generation Process

**Step-by-Step Process**:

1. **Create Payload**:
   ```json
   {
     "CompanyId": "550e8400-e29b-41d4-a716-446655440000",
     "ExpiryDate": "2025-12-31T23:59:59Z",
     "IssuedDate": "2025-01-15T10:00:00Z",
     "Version": 1
   }
   ```

2. **Generate HMAC Signature**:
   ```
   signature = HMAC_SHA256(payload_json, signing_key)
   ```

3. **Create Signed Token**:
   ```json
   {
     "Payload": { ... },
     "Signature": "base64_encoded_signature"
   }
   ```

4. **Encrypt with AES-256**:
   ```
   encrypted = AES_256_Encrypt(signed_token_json, encryption_key, iv)
   ```

5. **Base64 Encode**:
   ```
   license_key = Base64_Encode(encrypted_bytes)
   ```

**Result**: Secure, tamper-proof license key that can be validated offline

### License Key Validation Process

**Step-by-Step Validation**:

1. **Base64 Decode**: Convert license key string to bytes
2. **AES-256 Decrypt**: Decrypt bytes using encryption key and IV
3. **Parse JSON**: Extract payload and signature
4. **Verify HMAC**: Recompute signature and compare
5. **Check Expiry**: Verify `ExpiryDate >= Now`
6. **Detect Clock Tampering**: Verify `Now >= IssuedDate`
7. **Return Result**: Valid or Invalid with reason

**Clock Tampering Detection**:
- License key contains `IssuedDate`
- If current system time < `IssuedDate`, clock was tampered with
- Validation fails even if expiry date is valid

### Subscription Expiry Worker Logic

**Daily Execution Flow** (Default: 00:00 UTC):

1. **Query Expired Companies**:
   ```sql
   SELECT * FROM Companies 
   WHERE ExpiryDate < GETUTCDATE() 
   AND IsExpired = 0 
   AND IsDeleted = 0
   ```

2. **For Each Expired Company**:
   - Set `IsExpired = true`
   - Set `IsActive = false`
   - Create `Notification` (Type: Expired)
   - Send email notification (if email configured)
   - Trigger webhook (EventType: CompanyExpired)
   - Log to `SubscriptionHistory` (ActionType: Expired)
   - Update `UpdatedTimestamp`

3. **Query Trial Expired Companies**:
   ```sql
   SELECT * FROM Companies 
   WHERE IsTrial = 1 
   AND TrialEndDate < GETUTCDATE() 
   AND IsDeleted = 0
   ```

4. **For Each Trial Expired Company**:
   - Set `IsTrial = false`
   - Set `IsActive = false`
   - Create `Notification` (Type: Expired)
   - Trigger webhook (EventType: TrialExpired)
   - Log to `SubscriptionHistory` (ActionType: TrialExpired)

### Expiry Notification Worker Logic

**Daily Execution Flow** (Default: 09:00 UTC):

1. **For Each Warning Day** (7, 3, 1 days):
   - Query companies expiring in X days
   - For each company:
     - Create `Notification` (Type: ExpiryWarning)
     - Send email (if `ContactEmail` exists)
     - Include days remaining in message

2. **For Expired Today**:
   - Query companies where `ExpiryDate = Today`
   - Create `Notification` (Type: Expired)
   - Send email notification

### Webhook Delivery Process

**When Event Occurs**:

1. **Find Active Webhooks**:
   ```sql
   SELECT * FROM Webhooks 
   WHERE CompanyId = @companyId 
   AND IsActive = 1 
   AND JSON_CONTAINS(Events, @eventType)
   ```

2. **For Each Webhook**:
   - Create `WebhookDelivery` record
   - Build payload JSON:
     ```json
     {
       "eventType": "CompanyActivated",
       "companyId": "encrypted_id",
       "timestamp": "2025-01-15T10:00:00Z",
       "data": {
         "companyName": "Acme Corp",
         "expiryDate": "2025-12-31T23:59:59Z"
       }
     }
     ```
   - Compute HMAC signature: `HMAC_SHA256(payload, webhook_secret)`
   - POST to webhook URL with headers:
     - `X-Webhook-Signature: signature`
     - `X-Webhook-Event: CompanyActivated`
     - `Content-Type: application/json`
   - Update `WebhookDelivery`:
     - `StatusCode`: HTTP response code
     - `ResponseBody`: Response body (if any)
     - `Succeeded`: true if 200-299, false otherwise
     - `AttemptedAt`: Current timestamp

3. **Retry Logic** (If Failed):
   - Attempt 1: Immediate
   - Attempt 2: After 1 minute
   - Attempt 3: After 5 minutes
   - Attempt 4: After 30 minutes
   - Maximum 3 retries (configurable)

### API Key Authentication Flow

**Request Flow**:

1. **Extract API Key**:
   - Check `Authorization: Bearer {api_key}` header
   - Extract key string

2. **Hash Key**:
   ```
   key_hash = SHA256(api_key)
   ```

3. **Find API Key**:
   ```sql
   SELECT * FROM ApiKeys 
   WHERE KeyHash = @key_hash 
   AND IsActive = 1 
   AND IsDeleted = 0
   ```

4. **Validate**:
   - Check `ExpiresAt` (if set): Must be > Now
   - Check `AllowedIps`: If set, request IP must match (CIDR support)
   - Check rate limit: Query cache for request count
   - Update `LastUsedAt`

5. **Set Company Context**:
   - Extract `CompanyId` from API key
   - Set in request context for downstream services

### Rate Limiting Algorithm

**Sliding Window Implementation**:

1. **Key Generation**:
   ```
   cache_key = "ratelimit:{endpoint}:{api_key_id}:{window_start}"
   ```

2. **Window Calculation**:
   ```
   window_start = (current_timestamp / window_size) * window_size
   ```

3. **Check Limit**:
   ```
   current_count = cache.Get(cache_key)
   if (current_count >= limit):
       return 429 Too Many Requests
   ```

4. **Increment Counter**:
   ```
   cache.Increment(cache_key, expiration: window_size)
   ```

5. **Set Headers**:
   ```
   X-RateLimit-Limit: 1000
   X-RateLimit-Remaining: 999
   X-RateLimit-Reset: {window_end_timestamp}
   ```

### Password Policy Validation

**Validation Rules** (NIST 800-63B Compliant):

1. **Length Check**: `password.Length >= MinLength` (default: 8)
2. **Uppercase Check**: If `RequireUppercase`, must contain A-Z
3. **Lowercase Check**: If `RequireLowercase`, must contain a-z
4. **Number Check**: If `RequireNumbers`, must contain 0-9
5. **Special Char Check**: If `RequireSpecialChars`, must contain !@#$%^&*()
6. **History Check**: If `PreventReuseCount > 0`, check last N passwords
7. **Expiration Check**: If `MaxAgeDays > 0`, check password age

**Password History**:
- Stored as hashed passwords in `PasswordHistory` table
- Checked during password change
- Prevents reuse of last N passwords

### Failed Login Attempt Tracking

**Lockout Logic**:

1. **Record Attempt**:
   - Create `LoginAttempt` record
   - Store: Username, IP Address, Success/Failure, Timestamp

2. **Check Recent Failures**:
   ```sql
   SELECT COUNT(*) FROM LoginAttempts 
   WHERE Username = @username 
   AND Success = 0 
   AND AttemptedAt > DATEADD(MINUTE, -30, GETUTCDATE())
   ```

3. **Lockout Decision**:
   - If failures >= `MaxFailedAttempts` (default: 5):
     - Account is locked
     - Return error: "Account locked due to too many failed attempts"
   - Lockout duration: `LockoutDurationMinutes` (default: 30)

4. **Successful Login**:
   - Reset failure count
   - Clear lockout status

---

## Advanced Business Scenarios

### Scenario 1: Multi-Product Company

**Situation**: Company uses ERP, CRM, and POS systems, all checking same SYNFLOX company record.

**Flow**:
1. Admin creates company in SYNFLOX
2. Admin activates subscription
3. ERP system checks: `GET /api/licensing/{id}/status` → Active
4. CRM system checks: `GET /api/licensing/{id}/status` → Active
5. POS system checks: `GET /api/licensing/{id}/status` → Active
6. Admin suspends company
7. All three systems check status → Suspended
8. All three systems block access simultaneously

**Business Value**: Single point of control for all products

### Scenario 2: Trial to Paid Conversion

**Situation**: Company starts with 30-day trial, then converts to paid subscription.

**Flow**:
1. Admin starts trial: `POST /api/licensing/{id}/trial/start` (30 days)
2. Company uses system during trial
3. Day 25: Admin receives expiry warning notification
4. Day 28: Admin converts trial: `POST /api/licensing/{id}/trial/convert`
5. Trial ends, paid subscription begins seamlessly
6. No interruption to company's access

**Business Value**: Smooth conversion process, no downtime

### Scenario 3: Bulk Subscription Renewal

**Situation**: 100 companies need subscription extension at once.

**Flow**:
1. Admin selects 100 companies in admin panel
2. Admin calls: `POST /api/licensing/bulk-extend`
3. Request: `{ companyIds: [...], expiryDate: "2026-12-31" }`
4. System processes all 100 companies:
   - Updates `ExpiryDate` for each
   - Creates notifications
   - Triggers webhooks
   - Logs to history
5. Response: `{ totalRequested: 100, successful: 98, failed: 2 }`
6. Admin reviews failures and retries if needed

**Business Value**: Efficient bulk operations, time savings

### Scenario 4: Webhook Integration for Real-Time Updates

**Situation**: External system needs real-time notifications of subscription changes.

**Flow**:
1. External system registers webhook:
   ```http
   POST /api/webhooks
   {
     "url": "https://external-system.com/webhook",
     "events": ["CompanyActivated", "CompanySuspended", "CompanyExpired"]
   }
   ```

2. Admin suspends company in SYNFLOX
3. SYNFLOX triggers webhook:
   - POST to `https://external-system.com/webhook`
   - Payload: `{ eventType: "CompanySuspended", companyId: "...", ... }`
   - Header: `X-Webhook-Signature: hmac_signature`

4. External system receives webhook:
   - Validates HMAC signature
   - Processes suspension event
   - Updates local system state
   - Returns 200 OK

5. SYNFLOX logs successful delivery

**Business Value**: Real-time synchronization, no polling needed

### Scenario 5: API Key with IP Whitelisting

**Situation**: Company's ERP system should only access SYNFLOX from specific IPs.

**Flow**:
1. Admin creates API key with IP whitelist:
   ```http
   POST /api/api-keys
   {
     "companyId": "...",
     "allowedIps": ["192.168.1.0/24", "10.0.0.50"]
   }
   ```

2. ERP system uses API key from allowed IP: ✅ Success
3. ERP system uses API key from blocked IP: ❌ 403 Forbidden

**Business Value**: Enhanced security, IP-based access control

### Scenario 6: Subscription Plan with Modules

**Situation**: Company subscribes to "Professional" plan with specific modules.

**Flow**:
1. Admin creates plan: "Professional" ($99/month)
2. Admin creates project: "ERP System"
3. Admin creates modules: "Inventory", "Sales", "Reports"
4. Admin links modules to project
5. Admin assigns modules to plan:
   - Professional plan includes: ERP → Inventory, ERP → Sales
   - Professional plan excludes: ERP → Reports
6. Admin assigns plan to company
7. Company can access Inventory and Sales modules
8. Company cannot access Reports module (not in plan)

**Business Value**: Flexible feature control, tiered pricing

### Scenario 7: Company Group Bulk Operations

**Situation**: 50 companies in "Enterprise Clients" group need activation.

**Flow**:
1. Admin creates group: "Enterprise Clients"
2. Admin adds 50 companies to group
3. Admin calls: `POST /api/company-groups/{groupId}/bulk-activate`
4. System activates all 50 companies:
   - Updates each company's `IsActive = true`
   - Sets `ExpiryDate` for each
   - Creates notifications
   - Triggers webhooks
   - Logs to history
5. All 50 companies are activated in one operation

**Business Value**: Efficient group management, bulk operations

### Scenario 8: Custom Fields for Industry-Specific Data

**Situation**: Company needs to store industry-specific information.

**Flow**:
1. Admin creates custom fields for company:
   ```http
   POST /api/companies/{id}/custom-fields
   {
     "fieldName": "Industry",
     "fieldValue": "Manufacturing",
     "fieldType": "String"
   }
   
   POST /api/companies/{id}/custom-fields
   {
     "fieldName": "EmployeeCount",
     "fieldValue": "500",
     "fieldType": "Number"
   }
   ```

2. Custom fields stored in `CompanyCustomFields` table
3. Available for reporting and filtering
4. Can be exported in reports

**Business Value**: Flexible data storage, industry customization

### Scenario 9: Export/Import for Data Migration

**Situation**: Admin needs to migrate 1000 companies from old system.

**Flow**:
1. Admin exports companies from old system to Excel
2. Admin imports to SYNFLOX:
   ```http
   POST /api/companies/import
   Content-Type: multipart/form-data
   file: companies.xlsx
   ```

3. System validates each row:
   - Required fields present
   - Data types correct
   - No duplicates
4. System creates companies for valid rows
5. System reports errors for invalid rows
6. Response: `{ imported: 950, errors: 50, errorDetails: [...] }`

**Business Value**: Efficient data migration, bulk import

### Scenario 10: Analytics Dashboard for Business Intelligence

**Situation**: Admin needs to understand system usage and trends.

**Flow**:
1. Admin views dashboard: `GET /api/dashboard/overview`
2. Dashboard shows:
   - Total companies: 150
   - Active: 120, Expired: 20, Suspended: 10
   - API usage: 50,000 requests/day
   - Average response time: 45ms
   - Top endpoints: `/api/licensing/{id}/status`
3. Admin generates report: `POST /api/reports/UsageAnalytics/generate`
4. Report shows:
   - Usage by company
   - Usage by endpoint
   - Usage trends over time
5. Admin downloads report: `GET /api/reports/UsageAnalytics/download?format=excel`

**Business Value**: Data-driven decisions, business intelligence

---

## Complete Request/Response Examples

### Company Creation with Full Flow

**Request**:
```http
POST /api/companies
Authorization: Bearer {superadmin_token}
Content-Type: application/json
Accept-Language: en

{
  "name": "Acme Corporation",
  "expiryDate": "2025-12-31T23:59:59Z",
  "contactEmail": "admin@acme.com",
  "contactPhone": "+1234567890",
  "address": "123 Main St, City, Country",
  "subscriptionPlanId": "encrypted_plan_id"
}
```

**What Happens Behind the Scenes**:
1. Validate request (ModelState validation)
2. Decrypt `subscriptionPlanId` (if provided)
3. Create `Company` entity:
   - `IsActive = false` (default)
   - `IsExpired = false` (default)
   - `IsTrial = false` (default)
   - `CreatedTimestamp = DateTime.UtcNow`
4. Save to database
5. Log to `SubscriptionHistory`:
   - `ActionType = Created`
   - `OldValue = null`
   - `NewValue = { company data JSON }`
   - `PerformedBy = current_admin_id`
6. Map entity to DTO (encrypt ID)
7. Return response

**Response**:
```json
{
  "statusCode": 201,
  "message": "Company created successfully",
  "data": {
    "id": "encrypted_company_id",
    "name": "Acme Corporation",
    "isActive": false,
    "expiryDate": "2025-12-31T23:59:59Z",
    "contactEmail": "admin@acme.com",
    "contactPhone": "+1234567890",
    "address": "123 Main St, City, Country",
    "licenseKey": null,
    "isTrial": false,
    "trialEndDate": null,
    "subscriptionPlanId": "encrypted_plan_id",
    "createdAt": "2025-01-15T10:00:00Z",
    "updatedAt": null
  },
  "errors": []
}
```

### Activation with Notifications and Webhooks

**Request**:
```http
PUT /api/licensing/{encrypted_id}/activate
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

**What Happens Behind the Scenes**:
1. Decrypt company ID
2. Load company from database
3. Update company:
   - `IsActive = true`
   - `ExpiryDate = 2025-12-31T23:59:59Z`
   - `IsExpired = false` (if was expired)
   - `UpdatedTimestamp = DateTime.UtcNow`
4. Save to database
5. Create `Notification`:
   - `Type = Activated`
   - `Title = "Subscription Activated"`
   - `Message = "Your subscription has been activated"`
   - `CompanyId = company_id`
6. Send email (if `ContactEmail` exists):
   - Subject: "Subscription Activated"
   - Body: Localized email template
7. Trigger webhooks:
   - Find all active webhooks for company
   - Filter by event: `CompanyActivated`
   - For each webhook:
     - Create `WebhookDelivery` record
     - POST to webhook URL
     - Log delivery result
8. Log to `SubscriptionHistory`:
   - `ActionType = Activated`
   - `OldValue = { previous state JSON }`
   - `NewValue = { new state JSON }`
9. Map to DTO and return

**Response**:
```json
{
  "statusCode": 200,
  "message": "Company subscription activated successfully",
  "data": {
    "id": "encrypted_company_id",
    "name": "Acme Corporation",
    "isActive": true,
    "expiryDate": "2025-12-31T23:59:59Z",
    ...
  }
}
```

---

## Error Handling and Edge Cases

### Common Error Scenarios

#### 1. Company Not Found
**Request**: `GET /api/companies/{invalid_id}`

**Response**:
```json
{
  "statusCode": 404,
  "message": "Company not found",
  "data": null,
  "errors": ["Company with the specified ID was not found."]
}
```

**What Happens**:
- ID decryption fails or company doesn't exist
- `NotFoundException` thrown
- Exception middleware catches it
- Returns 404 with localized message

#### 2. Validation Error
**Request**: `POST /api/companies` with invalid data

**Response**:
```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "The Name field is required.",
    "The ContactEmail field is not a valid email address."
  ]
}
```

**What Happens**:
- ModelState validation fails
- Controller returns `BadRequest(ModelState)`
- Validation errors returned in `errors` array

#### 3. Rate Limit Exceeded
**Request**: Too many requests in short time

**Response**:
```http
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1642248000

{
  "statusCode": 429,
  "message": "Rate limit exceeded. Please try again later.",
  "data": null,
  "errors": ["You have exceeded the rate limit. Retry after 1642248000."]
}
```

**What Happens**:
- Rate limiting middleware checks cache
- Request count >= limit
- Returns 429 with retry-after information

#### 4. Unauthorized Access
**Request**: `POST /api/companies` without token

**Response**:
```http
HTTP/1.1 401 Unauthorized

{
  "statusCode": 401,
  "message": "Unauthorized",
  "data": null,
  "errors": ["Authentication required."]
}
```

#### 5. Forbidden Access
**Request**: `POST /api/companies` with Admin token (not SuperAdmin)

**Response**:
```http
HTTP/1.1 403 Forbidden

{
  "statusCode": 403,
  "message": "Forbidden",
  "data": null,
  "errors": ["You do not have permission to perform this action."]
}
```

### Edge Cases Handled

#### 1. Concurrent Activation
**Situation**: Two admins try to activate same company simultaneously

**Handling**:
- Database transaction isolation
- Last write wins (standard EF Core behavior)
- Both operations logged to history
- No data corruption

#### 2. Expired License Key Validation
**Situation**: Offline system validates expired license key

**Response**:
```json
{
  "statusCode": 401,
  "message": "License key has expired",
  "data": {
    "isValid": false,
    "status": "Expired",
    "expiryDate": "2024-01-01T00:00:00Z",
    "clockTampered": false,
    "message": "License key has expired"
  }
}
```

#### 3. Webhook Delivery Failure
**Situation**: External system webhook endpoint is down

**Handling**:
1. First attempt fails (timeout/error)
2. Retry after 1 minute
3. Retry after 5 minutes
4. Retry after 30 minutes
5. Mark as failed after max retries
6. Admin can manually retry: `POST /api/webhooks/retry-failed`

#### 4. Bulk Operation Partial Failure
**Situation**: Bulk activate 100 companies, 2 fail validation

**Response**:
```json
{
  "statusCode": 200,
  "message": "Bulk operation completed",
  "data": {
    "totalRequested": 100,
    "successful": 98,
    "failed": 2,
    "errorMessage": "2 companies failed: Invalid expiry date, Company not found"
  }
}
```

**What Happens**:
- System processes all companies
- Continues on individual failures
- Returns aggregate result
- Admin can review and retry failures

---

## Performance Considerations

### Database Optimization

**Indexes**:
- `Companies.Name` - Fast company search
- `Companies.LicenseKey` - Fast license key lookup (unique)
- `Companies.ExpiryDate` - Fast expiry queries
- `Companies.IsActive` - Fast active company queries
- `SubscriptionHistory.CompanyId` - Fast history queries
- `SubscriptionHistory.Timestamp` - Fast date range queries
- `ApiKeys.KeyHash` - Fast API key lookup (unique)

**Query Optimization**:
- Always filter by `IsDeleted = false`
- Use pagination for large result sets
- Use `AsNoTracking()` for read-only queries
- Use compiled queries for frequently executed queries

### Caching Strategy

**Response Caching**:
- Menu items cached (varies by user type)
- Dashboard statistics cached (5 minutes)
- Health check cached (1 minute)

**Distributed Caching** (Redis):
- Rate limit counters
- API key lookups
- Metrics aggregation

### Background Worker Optimization

**SubscriptionExpiryWorker**:
- Processes in batches (100 companies at a time)
- Uses database transactions
- Logs progress for monitoring

**ExpiryNotificationWorker**:
- Groups notifications by email domain
- Sends emails in batches
- Retries failed emails

---

## Monitoring and Alerting

### Key Metrics to Monitor

1. **Request Rate**: Requests per second
2. **Response Time**: P50, P95, P99 percentiles
3. **Error Rate**: 4xx and 5xx errors
4. **Active Companies**: Current count
5. **Expired Companies**: Count and trend
6. **API Key Usage**: Requests per API key
7. **Webhook Delivery Rate**: Success/failure ratio
8. **Database Query Time**: Slow query detection

### Health Check Monitoring

**Setup**:
- Monitor `/api/health` endpoint
- Check all health checks: database, redis, disk, email
- Alert if any check fails

**Example Monitoring Script**:
```bash
#!/bin/bash
response=$(curl -s http://synflox-api.com/api/health)
status=$(echo $response | jq -r '.status')

if [ "$status" != "Healthy" ]; then
    # Send alert
    echo "SYNFLOX health check failed: $status"
    exit 1
fi
```

---

## Troubleshooting Guide

### Common Issues and Solutions

#### Issue 1: Company Status Not Updating
**Symptoms**: Company shows Active but should be Expired

**Solutions**:
1. Check `SubscriptionExpiryWorker` is running
2. Check `ExpiryDate` in database
3. Manually trigger expiry check
4. Review `SubscriptionHistory` for recent actions

#### Issue 2: Webhooks Not Delivering
**Symptoms**: Webhooks created but not receiving events

**Solutions**:
1. Check webhook `IsActive = true`
2. Check webhook URL is accessible
3. Review `WebhookDeliveries` for error details
4. Test webhook URL manually
5. Check HMAC signature validation

#### Issue 3: Rate Limiting Too Aggressive
**Symptoms**: Legitimate requests being blocked

**Solutions**:
1. Review rate limit settings in `appsettings.json`
2. Increase limits for specific endpoints
3. Check API key rate limits
4. Review rate limit headers in responses

#### Issue 4: License Key Validation Failing
**Symptoms**: Valid license keys being rejected

**Solutions**:
1. Check system clock (clock tampering detection)
2. Verify encryption keys in configuration
3. Check license key format
4. Review validation logs

---

## Best Practices

### For Administrators

1. **Regular Status Reviews**: Check expired/suspended companies weekly
2. **Bulk Operations**: Use bulk operations for efficiency
3. **Trial Management**: Monitor trial conversions
4. **Webhook Testing**: Test webhooks before production use
5. **API Key Rotation**: Rotate API keys periodically
6. **Report Generation**: Generate reports regularly for insights

### For External System Developers

1. **Status Caching**: Cache status for 5-10 minutes to reduce API calls
2. **Error Handling**: Handle all status codes gracefully
3. **Retry Logic**: Implement exponential backoff for retries
4. **Webhook Validation**: Always validate HMAC signatures
5. **Rate Limit Respect**: Monitor rate limit headers
6. **Localization**: Use `Accept-Language` header for user messages

### For System Integrators

1. **API Key Security**: Store API keys securely (environment variables)
2. **Webhook Security**: Validate HMAC signatures
3. **Error Logging**: Log all API errors for debugging
4. **Monitoring**: Monitor webhook delivery success rates
5. **Testing**: Test all integration scenarios before production

---

## Future Enhancements (Roadmap)

### Planned Features

1. **Advanced Reporting**: Custom SQL report builder
2. **Multi-Currency Support**: Currency conversion
3. **Payment Integration**: Stripe/PayPal integration for automatic billing
4. **Advanced Analytics**: Machine learning for usage prediction
5. **API Versioning**: Support for multiple API versions
6. **GraphQL API**: Alternative to REST API
7. **Mobile App**: Native mobile app for admins
8. **Advanced Multi-Tenancy**: Full tenant isolation

---

## Glossary

- **Company**: A tenant/customer organization in SYNFLOX
- **Subscription**: The licensing agreement for a company
- **License Key**: Encrypted token for offline system validation
- **API Key**: Authentication token for API access
- **Webhook**: HTTP callback for event notifications
- **Plan**: Subscription tier with pricing and features
- **Project**: Product category (ERP, CRM, etc.)
- **Module**: Feature within a project
- **Trial**: Temporary free subscription period
- **Status**: Current subscription state (Active/Expired/Suspended)

---

## Appendix

### A. Complete Entity Relationship Diagram

```
Companies (1) ──< (N) SubscriptionHistory
Companies (1) ──< (N) Notifications
Companies (1) ──< (N) ApiKeys
Companies (1) ──< (N) Webhooks
Companies (1) ──< (N) CompanyCustomFields
Companies (N) >──< (N) CompanyGroups (via CompanyGroupMembers)
Companies (N) ──> (1) SubscriptionPlans

SubscriptionPlans (1) ──< (N) PlanProjectModules
PlanProjectModules (N) ──> (1) ProjectModules
ProjectModules (N) ──> (1) Projects
ProjectModules (N) ──> (1) Modules

Admins (1) ──< (N) SubscriptionHistory (PerformedBy)
Admins (N) ──> (1) AdminTypes
```

### B. Complete API Endpoint List

**Authentication** (3 endpoints):
- `POST /api/admin/auth/login`
- `POST /api/admin/auth/refresh-token`
- `POST /api/admin/auth/logout`

**Companies** (9 endpoints):
- `POST /api/companies`
- `GET /api/companies`
- `GET /api/companies/{id}`
- `PUT /api/companies/{id}`
- `DELETE /api/companies/{id}`
- `POST /api/companies/bulk-delete`
- `POST /api/companies/bulk-update`
- `GET /api/companies/export`
- `POST /api/companies/import`

**Licensing** (12 endpoints):
- `PUT /api/licensing/{id}/activate`
- `PUT /api/licensing/{id}/suspend`
- `PUT /api/licensing/{id}/resume`
- `PUT /api/licensing/{id}/extend`
- `GET /api/licensing/{id}/status`
- `POST /api/licensing/{id}/license-key/generate`
- `POST /api/licensing/{id}/license-key/regenerate`
- `POST /api/licensing/validate-key`
- `GET /api/licensing/{id}/history`
- `GET /api/licensing/history`
- `POST /api/licensing/{id}/trial/start`
- `POST /api/licensing/{id}/trial/convert`

**Bulk Operations** (4 endpoints):
- `POST /api/licensing/bulk-activate`
- `POST /api/licensing/bulk-suspend`
- `POST /api/licensing/bulk-resume`
- `POST /api/licensing/bulk-extend`

**Subscription Plans** (6 endpoints):
- `POST /api/subscription-plans`
- `GET /api/subscription-plans`
- `GET /api/subscription-plans/{id}`
- `PUT /api/subscription-plans/{id}`
- `DELETE /api/subscription-plans/{id}`
- `PUT /api/subscription-plans/{id}/project-modules`
- `GET /api/subscription-plans/{id}/project-modules`

**Projects** (5 endpoints):
- `POST /api/projects`
- `GET /api/projects`
- `GET /api/projects/{id}`
- `PUT /api/projects/{id}`
- `DELETE /api/projects/{id}`

**Modules** (5 endpoints):
- `POST /api/modules`
- `GET /api/modules`
- `GET /api/modules/{id}`
- `PUT /api/modules/{id}`
- `DELETE /api/modules/{id}`

**Project-Modules** (4 endpoints):
- `POST /api/project-modules`
- `GET /api/project-modules/project/{projectId}`
- `GET /api/project-modules/module/{moduleId}`
- `DELETE /api/project-modules/project/{projectId}/module/{moduleId}`

**Company Groups** (9 endpoints):
- `POST /api/company-groups`
- `GET /api/company-groups`
- `GET /api/company-groups/{id}`
- `PUT /api/company-groups/{id}`
- `DELETE /api/company-groups/{id}`
- `POST /api/company-groups/{id}/companies`
- `DELETE /api/company-groups/{id}/companies`
- `GET /api/company-groups/{id}/companies`
- `GET /api/company-groups/company/{companyId}`

**Group Bulk Operations** (4 endpoints):
- `POST /api/company-groups/{id}/bulk-activate`
- `POST /api/company-groups/{id}/bulk-suspend`
- `POST /api/company-groups/{id}/bulk-resume`
- `POST /api/company-groups/{id}/bulk-extend`

**Custom Fields** (5 endpoints):
- `GET /api/companies/{id}/custom-fields`
- `POST /api/companies/{id}/custom-fields`
- `GET /api/companies/{id}/custom-fields/{fieldId}`
- `PUT /api/companies/{id}/custom-fields/{fieldId}`
- `DELETE /api/companies/{id}/custom-fields/{fieldId}`

**Notifications** (5 endpoints):
- `GET /api/notifications`
- `GET /api/notifications/company/{companyId}`
- `GET /api/notifications/company/{companyId}/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/company/{companyId}/mark-all-read`

**API Keys** (7 endpoints):
- `POST /api/api-keys`
- `GET /api/api-keys`
- `GET /api/api-keys/company/{companyId}`
- `GET /api/api-keys/{id}`
- `PUT /api/api-keys/{id}`
- `DELETE /api/api-keys/{id}`
- `PUT /api/api-keys/{id}/regenerate`

**Webhooks** (7 endpoints):
- `POST /api/webhooks`
- `GET /api/webhooks`
- `GET /api/webhooks/company/{companyId}`
- `GET /api/webhooks/{id}`
- `DELETE /api/webhooks/{id}`
- `GET /api/webhooks/{id}/deliveries`
- `POST /api/webhooks/retry-failed`

**Analytics** (4 endpoints):
- `GET /api/analytics/company/{companyId}/usage`
- `GET /api/analytics/api-usage`
- `GET /api/analytics/api-usage/by-endpoint`
- `GET /api/analytics/api-usage/by-company`

**Reports** (3 endpoints):
- `GET /api/reports`
- `POST /api/reports/{reportType}/generate`
- `GET /api/reports/{reportType}/download`

**Metrics** (4 endpoints):
- `GET /api/metrics/summary`
- `GET /api/metrics/history`
- `POST /api/metrics/aggregate`
- `POST /api/metrics/cleanup`

**Error Logs** (3 endpoints):
- `GET /api/errors`
- `GET /api/errors/{errorId}`
- `POST /api/errors/cleanup`

**Password Policy** (3 endpoints):
- `GET /api/password-policy`
- `PUT /api/password-policy`
- `POST /api/password-policy/validate`

**Login Attempts** (2 endpoints):
- `GET /api/login-attempts`
- `GET /api/login-attempts/username/{username}/failed`

**Health Check** (1 endpoint):
- `GET /api/health`

**Dashboard** (3 endpoints):
- `GET /api/dashboard/endpoints`
- `GET /api/dashboard/statistics`
- `GET /api/dashboard/overview`

**Search** (5 endpoints):
- `POST /api/search`
- `GET /api/search`
- `GET /api/search/entity-types`
- `GET /api/search/suggestions`
- `GET /api/search/stats`

**File Upload** (5 endpoints):
- `POST /api/uploads/initiate`
- `PUT /api/uploads/{uploadId}/chunk`
- `GET /api/uploads/{uploadId}/status`
- `POST /api/uploads/{uploadId}/complete`
- `DELETE /api/uploads/{uploadId}`

**File Download** (5 endpoints):
- `POST /api/downloads/initiate`
- `GET /api/downloads/{downloadId}/status`
- `GET /api/downloads/{downloadId}/chunk`
- `GET /api/downloads/file`
- `GET /api/downloads/info`

**Admins** (18 endpoints):
- `POST /api/admins`
- `GET /api/admins`
- `GET /api/admins/{id}`
- `PUT /api/admins/{id}`
- `DELETE /api/admins/{id}`
- `PUT /api/admins/{id}/activate`
- `PUT /api/admins/{id}/deactivate`
- `PUT /api/admins/activate-selected`
- `PUT /api/admins/deactivate-selected`
- `PUT /api/admins/activate-all`
- `PUT /api/admins/deactivate-all`
- `POST /api/admins/{id}/reset-password`
- `DELETE /api/admins/selected`
- `DELETE /api/admins/all`
- `GET /api/admins/me`
- `PUT /api/admins/me`
- `PUT /api/admins/me/password`
- `DELETE /api/admins/me`

**Admin Types** (4 endpoints):
- `GET /api/admin-types`
- `GET /api/admin-types/{id}`
- `POST /api/admin-types`
- `PUT /api/admin-types/{id}`

**Menu Items** (5 endpoints):
- `GET /api/menuitems`
- `GET /api/menuitems/{id}`
- `POST /api/menuitems`
- `PUT /api/menuitems/{id}`
- `DELETE /api/menuitems/{id}`

**Tenants** (5 endpoints):
- `POST /api/tenants`
- `GET /api/tenants`
- `GET /api/tenants/{id}`
- `PUT /api/tenants/{id}`
- `DELETE /api/tenants/{id}`

**Total: 200+ HTTP endpoints across 24 controllers**

---

*This comprehensive documentation covers every aspect of the SYNFLOX system - from high-level business concepts to detailed technical implementation. It serves as a complete reference for administrators, developers, and system integrators working with SYNFLOX.*

**SYNFLOX** - Central Licensing System for Enterprise Products  
**Version**: 1.0  
**Status**: Production Ready (95% Complete)  
**Last Updated**: 2025-01-15  
**Total Documentation**: 3000+ lines covering all features, workflows, and technical details
