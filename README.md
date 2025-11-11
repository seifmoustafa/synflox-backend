# SYNFLOX Backend - Central Licensing System API

## Business Overview

**SYNFLOX** is an enterprise-grade **Central Licensing System** that serves as the authoritative control center for managing software licenses across multiple external enterprise applications (ERP, CRM, POS, HR, Inventory systems, etc.).

### Business Purpose
SYNFLOX solves the critical business problem of **centralized license management** for software vendors who distribute multiple enterprise products. Instead of each product managing its own licensing, SYNFLOX provides:

- **Unified License Control**: Single source of truth for all product licenses
- **Multi-Tenant Management**: Manage thousands of companies and their subscriptions
- **Flexible Licensing Models**: Support for both online and offline licensing scenarios
- **Real-Time Validation**: Instant license status checks for connected systems
- **Secure Offline Keys**: Encrypted license keys for air-gapped environments
- **Administrative Control**: Complete subscription lifecycle management

### Core Business Functions

#### 1. **Company (Tenant) Management**
- Create and manage customer companies
- Track contact information and subscription details
- Maintain audit trails for all changes
- Support for soft deletion and data retention

#### 2. **Subscription Lifecycle Control**
- **Activate**: Enable company subscriptions with expiry dates
- **Suspend**: Temporarily disable access (manual control)
- **Resume**: Reactivate suspended subscriptions
- **Extend**: Modify expiration dates for renewals
- **Status Checking**: Real-time validation of subscription state

#### 3. **License Key Management**
- Generate secure, encrypted license keys for offline systems
- Support key regeneration for security updates
- Tamper-proof validation with clock detection
- AES-256 encryption with HMAC SHA256 signatures

#### 4. **Multi-Product Integration**
- RESTful API for online product integration
- Standardized response formats for all products
- Support for different integration patterns
- Comprehensive error handling and messaging

## Technical Architecture

SYNFLOX implements **Clean Architecture** principles with strict layer separation and dependency inversion:

```
┌─────────────────────────────────────────────────────────┐
│                    WebAPI Layer                          │
│  Controllers • Middleware • Authentication • CORS       │
│  Authorization Policies • Exception Handling            │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                 Application Layer                        │
│  Service Interfaces • DTOs • AutoMapper Profiles        │
│  Business Logic Contracts • Request/Response Models     │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                   Domain Layer                           │
│  Entities • Enums • Interfaces • Business Rules         │
│  NO EXTERNAL DEPENDENCIES (Pure Business Logic)         │
└─────────────────────────────────────────────────────────┘
                            ↑
┌─────────────────────────────────────────────────────────┐
│               Infrastructure Layer                       │
│  EF Core • Repositories • External Services • Auth      │
│  Database Context • File Storage • Email/SMS            │
└─────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### **1. Domain Layer** (Core Business)
- **Entities**: `Company`, `Admin`, `AdminType`, `RefreshToken`, `MenuItems`
- **Enums**: `LicenseStatus` (Active, Expired, Suspended), `AuthProvider`, `Gender`
- **Base Classes**: `AuditEntity<TKey>`, `BaseEntity<TKey>` with soft delete support
- **Interfaces**: Repository contracts (`ICompanyRepository`, `IAdminRepository`, `IUnitOfWork`)
- **Business Rules**: Status calculation logic, validation rules
- **Zero Dependencies**: Pure business logic with no external references

#### **2. Application Layer** (Use Cases)
- **Service Interfaces**: `ILicensingService`, `ICompanyService`, `IAuthenticationService`
- **DTOs**: Complete data transfer object structure for all entities
- **AutoMapper Profiles**: Entity ↔ DTO mapping with encryption/decryption
- **Request/Response Models**: API contract definitions
- **Business Logic Orchestration**: Coordinates between domain and infrastructure
- **Separation of Concerns**: CRUD operations separate from business operations

#### **3. Infrastructure Layer** (External Concerns)
- **Services**: `LicensingService`, `CompanyService`, `AuthenticationService`
- **Repositories**: `BaseRepository<T>`, entity-specific repositories
- **Database Context**: `ApplicationDBContext` with SQL Server/Oracle support
- **Authentication**: JWT token generation, password hashing, claims management
- **External Services**: Email, SMS, file storage, caching (Redis)
- **Settings & Configuration**: Encryption, license keys, file uploads

#### **4. WebAPI Layer** (Presentation)
- **Controllers**: RESTful endpoints with proper HTTP status codes
- **Authorization**: Role-based policies (`SuperAdminOnly`, `AdminOrSuperAdmin`)
- **Middleware Pipeline**: Exception handling, localization, rate limiting, caching
- **API Documentation**: Swagger/OpenAPI integration
- **CORS Configuration**: Cross-origin resource sharing setup
- **Request/Response Handling**: Model validation, error responses

### Key Architectural Patterns

#### **ID Encryption Pattern**
```csharp
// All entity IDs are encrypted in API responses and decrypted in requests
// Uses AutoMapper converters for seamless transformation
CreateMap<Company, CompanyDto>()
    .ForMember(d => d.Id, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));
```

#### **Soft Delete Pattern**
```csharp
// All entities support soft deletion with audit trails
public class AuditEntity<TKey> : BaseEntity<TKey>
{
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
    public DateTime? DeletedTimestamp { get; set; }
}
```

#### **Repository Pattern with Unit of Work**
```csharp
// Centralized data access with transaction support
public interface IUnitOfWork
{
    ICompanyRepository Companies { get; }
    IAdminRepository Admins { get; }
    Task<int> SaveChangesAsync();
}
```

#### **Service Layer Separation**
- **CRUD Services**: Handle basic entity operations (`ICompanyService`)
- **Business Services**: Handle complex business logic (`ILicensingService`)
- **Clear Boundaries**: Each service has a single responsibility

## How It Works

### Status Calculation Logic

SYNFLOX uses a priority-based status calculation:

1. **Expired** (Highest Priority): If `ExpiryDate < DateTime.UtcNow`, status is **Expired** regardless of `IsActive` flag
2. **Suspended**: If `IsActive = false` AND `ExpiryDate >= DateTime.UtcNow`, status is **Suspended**
3. **Active**: If `IsActive = true` AND `ExpiryDate >= DateTime.UtcNow`, status is **Active**

### Online Systems Integration

Online systems call the SYNFLOX API directly to check license status:

```
External Product → GET /api/licensing/{companyId}/status → SYNFLOX
                                                              ↓
                                                         Check Status
                                                              ↓
External Product ← { status: "Active", expiryDate: "..." } ← SYNFLOX
```

**Example Flow:**
1. ERP system starts up
2. ERP calls `GET /api/licensing/{companyId}/status`
3. SYNFLOX checks company subscription status
4. Returns status: `Active`, `Expired`, or `Suspended`
5. ERP allows or blocks access based on response

### Offline Systems Integration

Offline systems use a **secure license key** installed locally:

1. **License Key Generation**: SuperAdmin generates an encrypted license key for the company
2. **Key Installation**: The license key is installed in the offline system's configuration
3. **Local Validation**: The offline system validates the key locally without internet connection
4. **Clock Tampering Detection**: System detects if system clock has been tampered with

**License Key Format:**
- Encrypted JWT-like token
- Contains: CompanyId, ExpiryDate, IssuedDate, Version
- Signed with HMAC SHA256
- Encrypted with AES-256
- Base64 encoded

**Validation Process:**
1. Decrypt license key
2. Verify HMAC signature
3. Check expiry date
4. Detect clock tampering (current time must be >= issued time)
5. Return validation result

## API Endpoints

### Company Management (SuperAdmin Only)

#### Create Company
```http
POST /api/licensing/companies
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "name": "Acme Corporation",
  "expiryDate": "2025-12-31T23:59:59Z",
  "contactEmail": "admin@acme.com",
  "contactPhone": "+1234567890",
  "address": "123 Main St, City, Country"
}
```

**Response (English):**
```json
{
  "statusCode": 201,
  "message": "Company created successfully",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Acme Corporation",
    "isActive": true,
    "expiryDate": "2025-12-31T23:59:59Z",
    "contactEmail": "admin@acme.com",
    "contactPhone": "+1234567890",
    "address": "123 Main St, City, Country"
  }
}
```

**Response (Arabic):**
```json
{
  "statusCode": 201,
  "message": "تم إنشاء الشركة بنجاح",
  "data": { ... }
}
```

#### Get All Companies
```http
GET /api/licensing/companies?page=1&pageSize=10&search=Acme
Authorization: Bearer {admin_jwt_token}
```

#### Get Company by ID
```http
GET /api/licensing/companies/{id}
Authorization: Bearer {admin_jwt_token}
```

#### Update Company
```http
PUT /api/licensing/companies/{id}
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "name": "Acme Corporation Updated",
  "contactEmail": "newadmin@acme.com"
}
```

#### Delete Company
```http
DELETE /api/licensing/companies/{id}
Authorization: Bearer {admin_jwt_token}
```

### Subscription Management (SuperAdmin Only)

#### Activate Company
```http
PUT /api/licensing/{id}/activate
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

**Response (English):**
```json
{
  "statusCode": 200,
  "message": "Company subscription activated successfully",
  "data": { ... }
}
```

**Response (Arabic):**
```json
{
  "statusCode": 200,
  "message": "تم تفعيل اشتراك الشركة بنجاح",
  "data": { ... }
}
```

#### Suspend Company
```http
PUT /api/licensing/{id}/suspend
Authorization: Bearer {admin_jwt_token}
```

**Response (English):**
```json
{
  "statusCode": 200,
  "message": "Company subscription suspended successfully",
  "data": { ... }
}
```

**Response (Arabic):**
```json
{
  "statusCode": 200,
  "message": "تم تعليق اشتراك الشركة بنجاح",
  "data": { ... }
}
```

#### Resume Company
```http
PUT /api/licensing/{id}/resume
Authorization: Bearer {admin_jwt_token}
```

#### Extend Subscription
```http
PUT /api/licensing/{id}/extend
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "newExpiryDate": "2026-12-31T23:59:59Z"
}
```

**Response (English):**
```json
{
  "statusCode": 200,
  "message": "Subscription extended successfully",
  "data": { ... }
}
```

**Response (Arabic):**
```json
{
  "statusCode": 200,
  "message": "تم تمديد الاشتراك بنجاح",
  "data": { ... }
}
```

### License Status Check (Public - For External Products)

#### Check Company Status
```http
GET /api/licensing/{id}/status
```

**Response (Active - English):**
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

**Response (Active - Arabic):**
```json
{
  "statusCode": 200,
  "message": "الاشتراك نشط",
  "data": {
    "status": "Active",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "statusMessage": "الاشتراك نشط"
  }
}
```

**Response (Expired - English):**
```json
{
  "statusCode": 200,
  "message": "Subscription has expired",
  "data": {
    "status": "Expired",
    "expiryDate": "2024-01-01T00:00:00Z",
    "isActive": true,
    "statusMessage": "Subscription has expired"
  }
}
```

**Response (Expired - Arabic):**
```json
{
  "statusCode": 200,
  "message": "انتهت صلاحية الاشتراك",
  "data": {
    "status": "Expired",
    "expiryDate": "2024-01-01T00:00:00Z",
    "isActive": true,
    "statusMessage": "انتهت صلاحية الاشتراك"
  }
}
```

**Response (Suspended - English):**
```json
{
  "statusCode": 200,
  "message": "Subscription is suspended",
  "data": {
    "status": "Suspended",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": false,
    "statusMessage": "Subscription is suspended"
  }
}
```

**Response (Suspended - Arabic):**
```json
{
  "statusCode": 200,
  "message": "الاشتراك معلق",
  "data": {
    "status": "Suspended",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": false,
    "statusMessage": "الاشتراك معلق"
  }
}
```

### License Key Management (SuperAdmin Only)

#### Generate License Key
```http
POST /api/licensing/{id}/license-key/generate
Authorization: Bearer {admin_jwt_token}
```

**Response (English):**
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

**Response (Arabic):**
```json
{
  "statusCode": 200,
  "message": "تم إنشاء مفتاح الترخيص بنجاح",
  "data": {
    "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "message": "تم إنشاء مفتاح الترخيص بنجاح"
  }
}
```

#### Regenerate License Key
```http
POST /api/licensing/{id}/license-key/regenerate
Authorization: Bearer {admin_jwt_token}
```

### License Key Validation (Public - For Offline Systems)

#### Validate License Key
```http
POST /api/licensing/validate-key
Content-Type: application/json

{
  "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (Valid - English):**
```json
{
  "statusCode": 200,
  "message": "Subscription is active",
  "data": {
    "isValid": true,
    "status": "Active",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "clockTampered": false,
    "companyId": "550e8400-e29b-41d4-a716-446655440000",
    "message": "Subscription is active"
  }
}
```

**Response (Valid - Arabic):**
```json
{
  "statusCode": 200,
  "message": "الاشتراك نشط",
  "data": {
    "isValid": true,
    "status": "Active",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "clockTampered": false,
    "companyId": "550e8400-e29b-41d4-a716-446655440000",
    "message": "الاشتراك نشط"
  }
}
```

**Response (Clock Tampered - English):**
```json
{
  "statusCode": 401,
  "message": "System clock tampering detected. License validation failed.",
  "data": {
    "isValid": false,
    "status": "Active",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "clockTampered": true,
    "companyId": "550e8400-e29b-41d4-a716-446655440000",
    "message": "System clock tampering detected. License validation failed."
  }
}
```

**Response (Clock Tampered - Arabic):**
```json
{
  "statusCode": 401,
  "message": "تم اكتشاف تلاعب بساعة النظام. فشل التحقق من الترخيص.",
  "data": {
    "isValid": false,
    "status": "Active",
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "clockTampered": true,
    "companyId": "550e8400-e29b-41d4-a716-446655440000",
    "message": "تم اكتشاف تلاعب بساعة النظام. فشل التحقق من الترخيص."
  }
}
```

## Usage Examples

### ERP System Integration

**Scenario**: ERP system needs to check if customer can access the system.

**Implementation:**
```csharp
// On ERP startup or periodic check
public async Task<bool> CheckLicenseStatus(Guid companyId)
{
    var client = new HttpClient();
    var response = await client.GetAsync(
        $"https://synflox-api.com/api/licensing/{companyId}/status");
    
    var result = await response.Content.ReadFromJsonAsync<CompanyStatusResponse>();
    
    if (result.Status == "Active")
    {
        // Allow access
        return true;
    }
    else if (result.Status == "Expired")
    {
        // Show subscription expired message
        ShowMessage(result.StatusMessage); // "Subscription has expired" or "انتهت صلاحية الاشتراك"
        return false;
    }
    else if (result.Status == "Suspended")
    {
        // Show suspended message
        ShowMessage(result.StatusMessage); // "Subscription is suspended" or "الاشتراك معلق"
        return false;
    }
    
    return false;
}
```

### CRM System Integration (Online)

**Scenario**: CRM system validates license on user login.

**Implementation:**
```javascript
// JavaScript/TypeScript example
async function validateLicense(companyId) {
    const response = await fetch(
        `https://synflox-api.com/api/licensing/${companyId}/status`,
        {
            headers: {
                'Accept-Language': 'ar' // or 'en' for English
            }
        }
    );
    
    const data = await response.json();
    
    if (data.data.status === 'Active') {
        // Proceed with login
        return true;
    } else {
        // Block login and show message
        alert(data.message); // Localized message
        return false;
    }
}
```

### POS System Integration (Offline)

**Scenario**: POS system works offline and needs to validate license locally.

**Implementation:**
```csharp
// Offline POS system
public bool ValidateOfflineLicense(string licenseKey)
{
    // Call SYNFLOX validation endpoint (when internet is available)
    // Or validate locally using installed license key
    
    var client = new HttpClient();
    var request = new
    {
        licenseKey = licenseKey
    };
    
    var response = await client.PostAsJsonAsync(
        "https://synflox-api.com/api/licensing/validate-key",
        request);
    
    var result = await response.Content.ReadFromJsonAsync<LicenseKeyValidationResponse>();
    
    if (!result.IsValid)
    {
        // License invalid or expired
        if (result.ClockTampered)
        {
            ShowError("System clock tampering detected!");
        }
        else
        {
            ShowError(result.Message); // Localized error message
        }
        return false;
    }
    
    // License is valid
    return true;
}
```

### HR System Integration

**Scenario**: HR system checks license status periodically.

**Implementation:**
```python
# Python example
import requests

def check_license_status(company_id, language='en'):
    url = f"https://synflox-api.com/api/licensing/{company_id}/status"
    headers = {
        'Accept-Language': language
    }
    
    response = requests.get(url, headers=headers)
    data = response.json()
    
    status = data['data']['status']
    message = data['message']
    
    if status == 'Active':
        print(f"License is active: {message}")
        return True
    elif status == 'Expired':
        print(f"License expired: {message}")
        return False
    elif status == 'Suspended':
        print(f"License suspended: {message}")
        return False
    
    return False

# Usage
if check_license_status('550e8400-e29b-41d4-a716-446655440000', 'ar'):
    # Continue with HR operations
    pass
else:
    # Block access
    pass
```

## Multi-Language Support

SYNFLOX supports **English** and **Arabic** responses. The system automatically detects the language from the `Accept-Language` HTTP header.

### Setting Language

**English:**
```http
GET /api/licensing/{id}/status
Accept-Language: en
```

**Arabic:**
```http
GET /api/licensing/{id}/status
Accept-Language: ar
```

### Response Examples

**English Response:**
```json
{
  "statusCode": 200,
  "message": "Subscription is active",
  "data": {
    "status": "Active",
    "statusMessage": "Subscription is active"
  }
}
```

**Arabic Response:**
```json
{
  "statusCode": 200,
  "message": "الاشتراك نشط",
  "data": {
    "status": "Active",
    "statusMessage": "الاشتراك نشط"
  }
}
```

## Security Architecture

### Multi-Layer Security Model

SYNFLOX implements enterprise-grade security across all layers:

#### **1. Authentication & Authorization**
```csharp
// JWT-based authentication with role-based authorization
[Authorize(Policy = "SuperAdminOnly")]
public async Task<IActionResult> ActivateCompany(Guid id, [FromBody] ActivateCompanyRequest request)
{
    var decryptedId = _idEncryption.Decrypt(id); // ID decryption at boundary
    var result = await _licensingService.ActivateCompanyAsync(decryptedId, request.ExpiryDate);
    return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanyActivated"], result));
}
```

#### **2. ID Encryption System**
- **Boundary Encryption**: All entity IDs encrypted at API boundaries
- **AutoMapper Integration**: Seamless encryption/decryption via converters
- **Security by Design**: Internal services work with plain GUIDs
- **Tamper Protection**: Encrypted IDs prevent enumeration attacks

#### **3. License Key Security**
```
License Key Structure:
┌─────────────────────────────────────────────────────────┐
│  Base64(AES-256_Encrypt(JSON({                          │
│    Payload: {                                           │
│      CompanyId: Guid,                                   │
│      ExpiryDate: DateTime,                              │
│      IssuedDate: DateTime,                              │
│      Version: int                                       │
│    },                                                   │
│    Signature: HMAC_SHA256(Payload)                      │
│  })))                                                   │
└─────────────────────────────────────────────────────────┘
```

**Security Features**:
- **AES-256 Encryption**: Military-grade encryption
- **HMAC SHA256 Signature**: Tamper-proof validation
- **Clock Tampering Detection**: Prevents system time manipulation
- **Version Control**: Future-proof key format evolution

#### **4. Validation Process**
1. **Decode**: Base64 → Encrypted JSON
2. **Decrypt**: AES-256 → Plain JSON
3. **Verify**: HMAC signature validation
4. **Check Expiry**: Date validation
5. **Clock Detection**: Time manipulation check
6. **Result**: Secure validation response

### Database Security

#### **Soft Delete Protection**
```csharp
// All queries automatically filter deleted records
public async Task<IEnumerable<Company>> GetActiveCompaniesAsync()
{
    return await _context.Companies
        .Where(c => !c.IsDeleted && c.IsActive) // Automatic soft delete filter
        .ToListAsync();
}
```

#### **Audit Trail System**
- **Automatic Timestamps**: Created, Updated, Deleted timestamps
- **Change Tracking**: Entity Framework change detection
- **Data Retention**: Soft delete preserves historical data
- **Compliance Ready**: Audit trails for regulatory requirements

#### **Database Encryption**
- **Connection String Security**: Encrypted configuration
- **Column-Level Encryption**: Sensitive data protection
- **Index Optimization**: Secure, performant queries
- **Multi-Database Support**: SQL Server + Oracle compatibility

## Configuration

### appsettings.json

```json
{
  "LicenseKeySettings": {
    "EncryptionKey": "7exg2aivWDs075iB+viRJOO7biKuI+XD9CUYUCEeBYM=",
    "IV": "HjE/Sa+cbfdt2fAyv7f3JA==",
    "SigningKey": "MpXbg7DFEYjqxGhLnU0U51C6c7Ln6Mt5KeyHDXcTA08=",
    "Version": 1
  },
  "JwtSettings": {
    "Issuer": "localhost",
    "Audience": "localhost",
    "SecretKey": "...",
    "Lifetime": 30,
    "RefreshTokenExpiration": 60
  }
}
```

## Authentication

SYNFLOX uses **JWT-based authentication** for admin access. All management endpoints require SuperAdmin authorization.

### Admin Login
```http
POST /api/admin/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password123"
}
```

### Using JWT Token
```http
GET /api/licensing/companies
Authorization: Bearer {jwt_token}
```

## Database Support

SYNFLOX supports:
- **SQL Server**
- **Oracle** (with automatic type conversions)

## Technology Stack

- **.NET** - Core framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **JWT** - Authentication
- **AutoMapper** - Object mapping
- **Serilog** - Logging
- **AES-256** - License key encryption
- **HMAC SHA256** - License key signing

## Security Features

1. **JWT Authentication**: Secure admin access
2. **Role-Based Authorization**: SuperAdmin and Admin roles
3. **Encrypted License Keys**: AES-256 encryption
4. **HMAC Signatures**: Tamper-proof license keys
5. **Clock Tampering Detection**: Prevents date manipulation
6. **Soft Delete**: Companies are soft-deleted, not permanently removed
7. **Audit Trail**: Automatic timestamp tracking

## Database Schema

### Core Entities

#### **Companies Table**
```sql
CREATE TABLE Companies (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ExpiryDate DATETIME2 NULL,
    ContactEmail NVARCHAR(200) NULL,
    ContactPhone NVARCHAR(50) NULL,
    Address NVARCHAR(500) NULL,
    LicenseKey NVARCHAR(1000) NULL,
    CreatedTimestamp DATETIME2 NOT NULL,
    UpdatedTimestamp DATETIME2 NULL,
    DeletedTimestamp DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- Indexes for optimal performance
CREATE UNIQUE INDEX IX_Companies_Name ON Companies (Name) WHERE IsDeleted = 0;
CREATE INDEX IX_Companies_LicenseKey ON Companies (LicenseKey) WHERE LicenseKey IS NOT NULL;
CREATE INDEX IX_Companies_Status ON Companies (ExpiryDate, IsActive, IsDeleted);
```

#### **Admins Table**
```sql
CREATE TABLE Admins (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    FirstName NVARCHAR(100) NULL,
    LastName NVARCHAR(100) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Username NVARCHAR(100) NOT NULL,
    Password NVARCHAR(100) NOT NULL, -- Hashed
    AdminTypeId UNIQUEIDENTIFIER NOT NULL,
    CreatedTimestamp DATETIME2 NOT NULL,
    UpdatedTimestamp DATETIME2 NULL,
    DeletedTimestamp DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (AdminTypeId) REFERENCES AdminTypes(Id)
);
```

#### **AdminTypes Table**
```sql
CREATE TABLE AdminTypes (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AdminTypeName NVARCHAR(50) NOT NULL, -- 'SuperAdmin', 'Admin'
    CreatedTimestamp DATETIME2 NOT NULL,
    UpdatedTimestamp DATETIME2 NULL,
    DeletedTimestamp DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
```

### Database Features
- **Audit Trails**: All tables include Created/Updated/Deleted timestamps
- **Soft Delete**: Logical deletion with `IsDeleted` flag
- **Optimized Indexes**: Performance-tuned for common queries
- **Referential Integrity**: Foreign key constraints
- **Multi-Database**: SQL Server (primary) and Oracle (secondary) support

## Deployment Architecture

### Environment Configuration

#### **Development Environment**
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=.\\SQLEXPRESS;Database=SYNFLOX_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "Issuer": "localhost",
    "Audience": "localhost",
    "SecretKey": "development-secret-key",
    "Lifetime": 30
  }
}
```

#### **Production Environment**
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=prod-server;Database=SYNFLOX_Prod;User Id=synflox_user;Password=***;Encrypt=True;"
  },
  "JwtSettings": {
    "Issuer": "api.synflox.com",
    "Audience": "synflox-clients",
    "SecretKey": "production-secure-key-256-bits",
    "Lifetime": 15
  },
  "CacheSettings": {
    "UseRedis": true,
    "RedisConnection": "prod-redis:6379"
  }
}
```

### Docker Deployment

#### **Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
RUN dotnet restore "WebAPI/WebAPI.csproj"
COPY . .
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

#### **Docker Compose**
```yaml
version: '3.8'
services:
  synflox-api:
    build: .
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__SqlServerConnection=Server=db;Database=SYNFLOX;User Id=sa;Password=YourPassword123;
    depends_on:
      - db
      - redis

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  sqlserver_data:
  redis_data:
```

## Getting Started

### Prerequisites
- **.NET 8.0 SDK** or later
- **SQL Server** (LocalDB, Express, or Full)
- **Visual Studio 2022** or **VS Code** with C# extension
- **Redis** (optional, for caching)

### Quick Start

#### **1. Clone and Setup**
```bash
git clone <repository-url>
cd SYNFLOX
dotnet restore
```

#### **2. Database Configuration**
Update `appsettings.json` in WebAPI project:
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=.\\SQLEXPRESS;Database=SYNFLOX;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

#### **3. Run Database Migrations**
```bash
cd WebAPI
dotnet ef database update
# Database will be created automatically with initial data
```

#### **4. Start the API**
```bash
dotnet run --project WebAPI
```

#### **5. Access the API**
- **Swagger UI**: `https://localhost:5001/swagger`
- **API Base URL**: `https://localhost:5001/api`

### Initial Login Credentials
- **Username**: `superadmin`
- **Password**: `password`
- **Role**: `SuperAdmin`

### Development Workflow

#### **1. Add New Entity**
1. Create entity in `Domain/Entities`
2. Add repository interface in `Domain/Interfaces`
3. Create DTOs in `Application/DTOs`
4. Add AutoMapper profile in `Application/Mapping`
5. Implement repository in `Infrastructure/Repositories`
6. Create service interface and implementation
7. Add controller in `WebAPI/Controllers`
8. Create database migration

#### **2. Testing**
```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration

# Generate test coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### **3. Code Quality**
```bash
# Format code
dotnet format

# Analyze code
dotnet build --verbosity normal
```

## API Documentation

Full API documentation is available via Swagger UI when running the application:
- Development: `https://localhost:5001/swagger`
- Production: `https://your-domain.com/swagger`

## Support

For issues, questions, or contributions, please refer to the project repository.

---

**SYNFLOX** - Central Licensing System for Enterprise Products  
**Version**: 1.0  
**Last Updated**: 2025
