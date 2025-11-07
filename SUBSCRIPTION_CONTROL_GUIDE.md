# SYNFLOX - Subscription Control Guide

## 🎯 How SYNFLOX Controls External Systems

SYNFLOX is a **Central Licensing System** that controls subscription access for external enterprise products (ERP, CRM, POS, HR, Inventory systems, etc.).

---

## 📋 Complete Control Workflow

### Step 1: Create Company (Tenant)

**Action**: Create a company in SYNFLOX that represents the external system's customer.

**Endpoint**: `POST /api/companies`

**What You Control**:
- Company name
- Initial expiry date (optional)
- Contact information

**Example**:
```http
POST /api/companies
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "name": "Acme Corporation",
  "expiryDate": "2025-12-31T23:59:59Z",
  "contactEmail": "admin@acme.com",
  "contactPhone": "+1234567890",
  "address": "123 Main St, City, Country"
}
```

**Result**: Company is created with `IsActive = false` by default (not activated yet).

---

### Step 2: Activate Subscription

**Action**: Activate the company's subscription to allow access.

**Endpoint**: `PUT /api/licensing/{companyId}/activate`

**What You Control**:
- Set subscription as **Active**
- Set expiry date
- External system can now access their system

**Example**:
```http
PUT /api/licensing/{encryptedCompanyId}/activate
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

**Result**: 
- `IsActive = true`
- `ExpiryDate = 2025-12-31T23:59:59Z`
- Status = **Active**
- External system can now access

---

### Step 3: External System Checks Status

**How External System Integrates**:

The external system (ERP/CRM/POS/etc.) calls SYNFLOX to check if they're allowed to run:

```http
GET /api/licensing/{encryptedCompanyId}/status
Accept-Language: en
```

**Response (Active)**:
```json
{
  "statusCode": 200,
  "message": "Subscription is active",
  "data": {
    "status": 1,  // Active
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "statusMessage": "Subscription is active"
  }
}
```

**External System Logic**:
```csharp
// In external system (ERP/CRM/etc.)
var statusResponse = await CheckLicenseStatus(companyId);

if (statusResponse.Status == LicenseStatus.Active)
{
    // ✅ ALLOW ACCESS - System can run normally
    AllowUserAccess();
}
else if (statusResponse.Status == LicenseStatus.Expired)
{
    // ❌ BLOCK ACCESS - Show expiry message
    BlockAccess(statusResponse.StatusMessage);
    // Message: "Subscription has expired" or "انتهت صلاحية الاشتراك"
}
else if (statusResponse.Status == LicenseStatus.Suspended)
{
    // ❌ BLOCK ACCESS - Show suspension message
    BlockAccess(statusResponse.StatusMessage);
    // Message: "Subscription is suspended" or "الاشتراك معلق"
}
```

---

## 🎮 Control Actions Available

### 1. **Activate Subscription**
**When**: New subscription or reactivating expired/suspended subscription

**Action**: `PUT /api/licensing/{id}/activate`

**What Happens**:
- Sets `IsActive = true`
- Sets `ExpiryDate` to specified date
- Status becomes **Active**
- External system can now access

**Use Case**: 
- New customer subscription
- Renewal after expiry
- Reactivation after suspension

---

### 2. **Suspend Subscription**
**When**: Temporarily block access (e.g., payment issue, violation)

**Action**: `PUT /api/licensing/{id}/suspend`

**What Happens**:
- Sets `IsActive = false`
- Status becomes **Suspended**
- External system is immediately blocked
- Expiry date remains unchanged

**Use Case**:
- Payment overdue
- Terms of service violation
- Temporary suspension

**External System Response**:
```json
{
  "statusCode": 200,
  "message": "Subscription is suspended",
  "data": {
    "status": 3,  // Suspended
    "isActive": false,
    "statusMessage": "Subscription is suspended"
  }
}
```

---

### 3. **Resume Subscription**
**When**: Reactivate after suspension (payment received, issue resolved)

**Action**: `PUT /api/licensing/{id}/resume`

**What Happens**:
- Sets `IsActive = true`
- Status becomes **Active** (if not expired)
- External system can access again

**Use Case**:
- Payment received after suspension
- Issue resolved

**Note**: Cannot resume if subscription is expired. Must use **Activate** with new expiry date.

---

### 4. **Extend Subscription**
**When**: Extend expiry date (renewal, upgrade)

**Action**: `PUT /api/licensing/{id}/extend`

**What Happens**:
- Updates `ExpiryDate` to new date
- Status remains **Active** (if currently active)
- External system continues access

**Use Case**:
- Subscription renewal
- Upgrade to longer period
- Extend before expiry

**Example**:
```http
PUT /api/licensing/{id}/extend
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "newExpiryDate": "2026-12-31T23:59:59Z"
}
```

---

### 5. **Check Status** (For External Systems)
**When**: External system needs to verify access

**Action**: `GET /api/licensing/{id}/status` (Public, no auth required)

**What Happens**:
- Returns current subscription status
- Returns expiry date
- Returns localized message

**Response Types**:
- **Active**: System can run
- **Expired**: Subscription expired, block access
- **Suspended**: Subscription suspended, block access

---

## 🔐 Status Calculation Logic

SYNFLOX uses **priority-based status calculation**:

### Priority Order:
1. **Expired** (Highest Priority)
   - If `ExpiryDate < DateTime.UtcNow` → Status = **Expired**
   - Even if `IsActive = true`, if expired, status is **Expired**

2. **Suspended**
   - If `IsActive = false` AND `ExpiryDate >= DateTime.UtcNow` → Status = **Suspended**

3. **Active**
   - If `IsActive = true` AND `ExpiryDate >= DateTime.UtcNow` → Status = **Active**

### Examples:

| IsActive | ExpiryDate | Current Date | Status |
|----------|------------|--------------|--------|
| `true` | `2025-12-31` | `2025-01-15` | **Active** ✅ |
| `true` | `2024-12-31` | `2025-01-15` | **Expired** ❌ |
| `false` | `2025-12-31` | `2025-01-15` | **Suspended** ❌ |
| `false` | `2024-12-31` | `2025-01-15` | **Expired** ❌ |

---

## 🌐 Two Integration Methods

### Method 1: Online Systems (Internet Required)

**For**: Systems with internet connection (cloud ERP, SaaS CRM, etc.)

**How It Works**:
1. External system calls `GET /api/licensing/{companyId}/status` on startup/login
2. SYNFLOX checks database and returns status
3. External system allows/blocks based on response

**Advantages**:
- ✅ Real-time control (immediate suspension/activation)
- ✅ No license key management
- ✅ Can check status anytime
- ✅ Automatic expiry detection

**Implementation Example** (C#):
```csharp
public class LicenseChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _synfloxApiUrl = "https://api.synflox.com";
    private readonly Guid _companyId;

    public async Task<bool> CanAccess()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_synfloxApiUrl}/api/licensing/{_companyId}/status");
            
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyStatusResponse>>();
            
            return result.Data.Status == LicenseStatus.Active;
        }
        catch
        {
            // If SYNFLOX is unreachable, decide policy:
            // Option 1: Block access (strict)
            // Option 2: Allow with warning (lenient)
            return false; // Strict: block if can't verify
        }
    }
}
```

**Implementation Example** (JavaScript):
```javascript
async function checkLicenseStatus(companyId) {
  try {
    const response = await fetch(
      `https://api.synflox.com/api/licensing/${companyId}/status`,
      {
        headers: {
          'Accept-Language': 'en' // or 'ar'
        }
      }
    );
    
    const result = await response.json();
    
    if (result.data.status === 1) { // Active
      return { allowed: true, message: result.message };
    } else {
      return { 
        allowed: false, 
        message: result.message,
        status: result.data.status 
      };
    }
  } catch (error) {
    // Handle error - block or allow with warning
    return { allowed: false, message: 'License check failed' };
  }
}
```

---

### Method 2: Offline Systems (No Internet)

**For**: Systems without internet (on-premise ERP, local POS, etc.)

**How It Works**:
1. SuperAdmin generates encrypted license key in SYNFLOX
2. License key is installed in offline system's config file
3. Offline system validates key locally (no internet needed)
4. Key contains expiry date, company ID, and tamper detection

**Advantages**:
- ✅ Works without internet
- ✅ Secure (encrypted, signed)
- ✅ Detects clock tampering
- ✅ Self-contained validation

**Disadvantages**:
- ❌ Requires key regeneration for changes
- ❌ Cannot suspend immediately (must regenerate key)
- ❌ Key must be manually installed

**Step-by-Step**:

#### Step 1: Generate License Key (In SYNFLOX)
```http
POST /api/licensing/{companyId}/license-key/generate
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

#### Step 2: Install Key in Offline System
Save the `licenseKey` to configuration file:
```json
{
  "license": {
    "key": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

#### Step 3: Validate Key (In Offline System)
```http
POST /api/licensing/validate-key
Content-Type: application/json

{
  "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (Valid)**:
```json
{
  "statusCode": 200,
  "message": "Subscription is active",
  "data": {
    "isValid": true,
    "status": 1,  // Active
    "expiryDate": "2025-12-31T23:59:59Z",
    "isActive": true,
    "clockTampered": false,
    "companyId": "...",
    "message": "Subscription is active"
  }
}
```

**Response (Clock Tampered)**:
```json
{
  "statusCode": 401,
  "message": "System clock tampering detected. License validation failed.",
  "data": {
    "isValid": false,
    "clockTampered": true,
    "message": "System clock tampering detected. License validation failed."
  }
}
```

**Note**: For truly offline systems, you need to implement the validation logic in the external system itself (decrypt, verify signature, check expiry, detect clock tampering).

---

## 📊 Complete Control Scenarios

### Scenario 1: New Customer Subscription

**Steps**:
1. ✅ Create company: `POST /api/companies`
2. ✅ Activate subscription: `PUT /api/licensing/{id}/activate` with expiry date
3. ✅ External system checks status: `GET /api/licensing/{id}/status`
4. ✅ External system allows access (status = Active)

**Result**: Customer can use the system until expiry date.

---

### Scenario 2: Suspend Due to Payment Issue

**Steps**:
1. ✅ Suspend subscription: `PUT /api/licensing/{id}/suspend`
2. ✅ External system checks status (on next request/login)
3. ✅ Status = Suspended
4. ✅ External system blocks access and shows message

**Result**: Customer cannot access until payment received.

---

### Scenario 3: Payment Received - Resume Access

**Steps**:
1. ✅ Resume subscription: `PUT /api/licensing/{id}/resume`
2. ✅ External system checks status
3. ✅ Status = Active
4. ✅ External system allows access

**Result**: Customer can access again.

---

### Scenario 4: Subscription Renewal

**Steps**:
1. ✅ Extend subscription: `PUT /api/licensing/{id}/extend` with new expiry date
2. ✅ External system continues checking status
3. ✅ Status remains Active
4. ✅ External system continues access

**Result**: Subscription extended, customer continues using system.

---

### Scenario 5: Subscription Expired

**What Happens Automatically**:
1. ✅ External system checks status: `GET /api/licensing/{id}/status`
2. ✅ SYNFLOX calculates: `ExpiryDate < DateTime.UtcNow` → Status = **Expired**
3. ✅ External system receives status = Expired
4. ✅ External system blocks access

**To Reactivate**:
1. ✅ Activate with new expiry: `PUT /api/licensing/{id}/activate`
2. ✅ Status becomes Active
3. ✅ External system allows access

---

## 🔄 Real-Time Control Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    SYNFLOX Admin Panel                        │
│                                                               │
│  [Activate] [Suspend] [Resume] [Extend] [Generate Key]      │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ API Calls
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                    SYNFLOX Backend                          │
│                                                               │
│  • Updates Company.IsActive                                 │
│  • Updates Company.ExpiryDate                                │
│  • Calculates Status (Active/Expired/Suspended)              │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Status Check
                        │
        ┌───────────────┴───────────────┐
        │                               │
        ▼                               ▼
┌───────────────┐              ┌───────────────┐
│ Online System │              │ Offline System│
│  (ERP/CRM)    │              │  (Local POS)  │
│               │              │               │
│ GET /status   │              │ Validate Key  │
│               │              │               │
│ Allow/Block   │              │ Allow/Block   │
└───────────────┘              └───────────────┘
```

---

## 🎯 Key Points

### What You Control:
1. ✅ **Activation** - When subscription becomes active
2. ✅ **Suspension** - Temporarily block access
3. ✅ **Resumption** - Reactivate after suspension
4. ✅ **Expiry Date** - When subscription expires
5. ✅ **Extension** - Extend subscription period

### What External Systems Do:
1. ✅ **Check Status** - Call `/api/licensing/{id}/status` periodically
2. ✅ **Allow/Block** - Based on status response
3. ✅ **Show Messages** - Display localized status messages

### Status Priority:
1. **Expired** (highest) - If date passed, always expired
2. **Suspended** - If IsActive = false and not expired
3. **Active** - If IsActive = true and not expired

---

## 📝 Quick Reference

### Control Endpoints (SuperAdmin Only):
```http
PUT /api/licensing/{id}/activate      # Activate subscription
PUT /api/licensing/{id}/suspend       # Suspend subscription
PUT /api/licensing/{id}/resume        # Resume subscription
PUT /api/licensing/{id}/extend        # Extend expiry date
POST /api/licensing/{id}/license-key/generate  # Generate key (offline)
```

### Status Check Endpoint (Public):
```http
GET /api/licensing/{id}/status        # Check subscription status (no auth)
```

### License Key Validation (Public):
```http
POST /api/licensing/validate-key     # Validate license key (no auth)
```

---

## 🚀 Implementation Checklist for External Systems

### For Online Systems:
- [ ] Call `GET /api/licensing/{companyId}/status` on startup
- [ ] Call periodically (e.g., every hour or on user login)
- [ ] Block access if status ≠ Active
- [ ] Show localized message from `statusMessage` field
- [ ] Handle network errors (decide: block or allow with warning)

### For Offline Systems:
- [ ] Install license key in configuration
- [ ] Validate key on startup
- [ ] Implement local validation (decrypt, verify, check expiry)
- [ ] Detect clock tampering
- [ ] Block access if invalid or expired
- [ ] Show appropriate messages

---

## 💡 Best Practices

1. **Check Status Regularly**: External systems should check status periodically (not just on startup)
2. **Handle Errors Gracefully**: If SYNFLOX is unreachable, decide policy (block or allow with warning)
3. **Show User-Friendly Messages**: Use `statusMessage` from response (localized)
4. **Cache Status Temporarily**: Cache status for a few minutes to reduce API calls
5. **Log Status Checks**: Log all status checks for auditing
6. **Monitor Expiry**: Warn users before expiry (e.g., 7 days before)

---

## 📞 Summary

**To Control External Systems:**

1. **Create Company** → `POST /api/companies`
2. **Activate** → `PUT /api/licensing/{id}/activate` (sets Active)
3. **Suspend** → `PUT /api/licensing/{id}/suspend` (blocks immediately)
4. **Resume** → `PUT /api/licensing/{id}/resume` (reactivates)
5. **Extend** → `PUT /api/licensing/{id}/extend` (renews)

**External Systems:**
- Call `GET /api/licensing/{id}/status` to check access
- Allow if status = Active
- Block if status = Expired or Suspended

**That's it!** SYNFLOX controls access, external systems check status. 🎯

