# SYNFLOX - Central Licensing System

## Overview

**SYNFLOX** is a **Central Licensing System** (activation control system) designed to manage and control licensing for multiple external enterprise products including ERP, CRM, POS, HR, Inventory systems, and more.

SYNFLOX acts as the central authority that:
- Creates and manages companies (tenants)
- Controls subscription activation status (Active / Suspended)
- Manages subscription expiration dates
- Allows subscription extension
- Provides real-time license validation for external products
- Supports both **online** and **offline** system licensing

## Architecture

SYNFLOX follows **Clean Architecture** principles with a 4-layer structure:

```
┌─────────────────────────────────────┐
│         WebAPI (Presentation)        │  ← Controllers, Middleware, Configuration
├─────────────────────────────────────┤
│      Application (Business Logic)   │  ← DTOs, Service Interfaces, Mappings
├─────────────────────────────────────┤
│      Domain (Core Business)         │  ← Entities, Interfaces, Enums, Exceptions
├─────────────────────────────────────┤
│   Infrastructure (External Concerns)│  ← EF Core, Repositories, Services, Auth
└─────────────────────────────────────┘
```

### Key Components

#### **Domain Layer**
- **Company Entity**: Represents a tenant with subscription information
- **LicenseStatus Enum**: Active, Expired, Suspended
- **Repository Interfaces**: Data access contracts

#### **Application Layer**
- **DTOs**: Data transfer objects for API communication
- **ILicensingService**: Business logic interface
- **AutoMapper Profiles**: Entity to DTO mapping

#### **Infrastructure Layer**
- **CompanyRepository**: Data access implementation
- **LicensingService**: Core licensing business logic
- **License Key Generation**: Secure encrypted license key creation
- **License Key Validation**: Tamper-proof validation with clock detection

#### **WebAPI Layer**
- **LicensingController**: REST API endpoints
- **Authentication**: JWT-based admin authentication
- **Localization**: Multi-language support (English/Arabic)

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

## License Key Security

### Encryption & Signing

License keys use multiple layers of security:

1. **HMAC SHA256 Signature**: Prevents tampering
2. **AES-256 Encryption**: Protects key content
3. **Clock Tampering Detection**: Detects system date manipulation
4. **Version Control**: Supports future key format updates

### Key Structure

```
License Key = Base64(AES-256_Encrypt(JSON({
    Payload: {
        CompanyId: Guid,
        ExpiryDate: DateTime,
        IssuedDate: DateTime,
        Version: int
    },
    Signature: HMAC_SHA256(Payload)
})))
```

### Validation Process

1. Decode from Base64
2. Decrypt with AES-256
3. Verify HMAC signature
4. Check expiry date
5. Detect clock tampering (current time >= issued time)
6. Return validation result

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

## Getting Started

1. **Configure Database**: Update connection string in `appsettings.json`
2. **Configure License Keys**: Update `LicenseKeySettings` in `appsettings.json`
3. **Run Migrations**: Database will be created automatically
4. **Create SuperAdmin**: Use database initializer or create manually
5. **Start API**: Run the WebAPI project
6. **Access Swagger**: Navigate to `/swagger` for API documentation

## API Documentation

Full API documentation is available via Swagger UI when running the application:
- Development: `https://localhost:5001/swagger`
- Production: `https://your-domain.com/swagger`

## Support

For issues, questions, or contributions, please refer to the project repository.

---

**SYNFLOX** - Central Licensing System for Enterprise Products  
**Version**: 1.0  
**Last Updated**: 2024
