# SYNFLOX External System Integration - Complete Business Flow Guide

## 📋 Overview

This guide documents **every integration flow** for external systems (ERP, CRM, POS, HR, Inventory systems, etc.) that need to check license status, validate license keys, and receive webhooks from SYNFLOX.

**Target Audience**: External System Developers, Integration Engineers, System Architects  
**Purpose**: Complete understanding of how to integrate with SYNFLOX licensing system

---

## 🎯 System Purpose

SYNFLOX is a **Central Licensing System** that controls access to your enterprise software. External systems must:

1. **Check License Status** - Verify if subscription is active before allowing access
2. **Validate License Keys** - For offline systems without internet access
3. **Receive Webhooks** - Get notified of subscription changes (expiry, suspension, etc.)
4. **Authenticate Requests** - Use API keys for secure API access

---

## 🔄 Integration Methods

### Method 1: Online Licensing (Real-Time API Calls)

**Best For**: Systems with internet connectivity  
**How It Works**: System calls SYNFLOX API to check license status in real-time

### Method 2: Offline Licensing (License Key Validation)

**Best For**: Systems without internet access or air-gapped environments  
**How It Works**: System validates encrypted license key locally (no API calls needed)

### Method 3: Hybrid (Online + Offline Fallback)

**Best For**: Systems that prefer online but need offline fallback  
**How It Works**: Try online first, fallback to license key validation if offline

---

## 🔐 Authentication Methods

### 1. API Key Authentication

**Header**: `Authorization: Bearer {api_key}`

**How to Get API Key**:
1. Admin creates API key in SYNFLOX Admin Panel
2. Admin copies API key (shown only once)
3. Admin provides key to external system developer
4. External system stores key securely (environment variable, secure vault)

**Example**:
```http
GET /api/licensing/{company_id}/status
Authorization: Bearer sk_live_abc123xyz789...
```

### 2. HMAC Request Signing (Optional, Enhanced Security)

**Headers**:
- `X-Signature: {hmac_signature}`
- `X-Timestamp: {unix_timestamp}`

**Algorithm**: HMAC SHA256

**Signature Format**:
```
signature = HMAC-SHA256(timestamp + method + path + body, signing_secret)
```

**Example**:
```http
POST /api/licensing/validate-key
X-Signature: a1b2c3d4e5f6...
X-Timestamp: 1705320000
Authorization: Bearer sk_live_abc123xyz789...
Content-Type: application/json

{ "licenseKey": "..." }
```

---

## 📊 Complete Integration Flows

### Flow 1: Online Licensing - Real-Time Status Check

#### Step 1: System Startup - Initial Status Check

**What Happens**:
1. External system starts up (ERP/CRM/POS application)
2. System reads company ID from configuration
3. System calls SYNFLOX API to check license status
4. System receives status response
5. System allows or blocks access based on status

**API Endpoint**: `GET /api/licensing/{encrypted_company_id}/status`

**Request**:
```http
GET /api/licensing/eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.../status
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

**Response (Expired)**:
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

**Response (Suspended)**:
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

**System Action**:
- **Active**: Allow access, show normal UI
- **Expired**: Block access, show "Subscription Expired" message, redirect to renewal page
- **Suspended**: Block access, show "Subscription Suspended" message, contact admin

**Code Example (C#)**:
```csharp
public class LicenseChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _companyId;
    private readonly string _apiKey;

    public LicenseChecker(string companyId, string apiKey)
    {
        _companyId = companyId;
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en");
    }

    public async Task<LicenseStatus> CheckStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://api.synflox.com/api/licensing/{_companyId}/status"
            );
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<StatusResponse>>(content);
                
                return result.Data.Status switch
                {
                    "Active" => LicenseStatus.Active,
                    "Expired" => LicenseStatus.Expired,
                    "Suspended" => LicenseStatus.Suspended,
                    _ => LicenseStatus.Unknown
                };
            }
            
            return LicenseStatus.Unknown;
        }
        catch (Exception ex)
        {
            // Handle offline scenario - fallback to license key validation
            return LicenseStatus.Offline;
        }
    }
}
```

**Code Example (JavaScript/Node.js)**:
```javascript
class LicenseChecker {
    constructor(companyId, apiKey) {
        this.companyId = companyId;
        this.apiKey = apiKey;
        this.baseUrl = 'https://api.synflox.com';
    }

    async checkStatus() {
        try {
            const response = await fetch(
                `${this.baseUrl}/api/licensing/${this.companyId}/status`,
                {
                    method: 'GET',
                    headers: {
                        'Authorization': `Bearer ${this.apiKey}`,
                        'Accept-Language': 'en'
                    }
                }
            );

            if (response.ok) {
                const result = await response.json();
                return result.data.status; // "Active", "Expired", or "Suspended"
            }
            
            return 'Unknown';
        } catch (error) {
            // Handle offline scenario
            return 'Offline';
        }
    }
}
```

**Code Example (Python)**:
```python
import requests
from typing import Optional

class LicenseChecker:
    def __init__(self, company_id: str, api_key: str):
        self.company_id = company_id
        self.api_key = api_key
        self.base_url = "https://api.synflox.com"
        self.headers = {
            "Authorization": f"Bearer {api_key}",
            "Accept-Language": "en"
        }

    def check_status(self) -> Optional[str]:
        try:
            response = requests.get(
                f"{self.base_url}/api/licensing/{self.company_id}/status",
                headers=self.headers,
                timeout=5
            )
            
            if response.status_code == 200:
                result = response.json()
                return result["data"]["status"]  # "Active", "Expired", or "Suspended"
            
            return None
        except requests.RequestException:
            # Handle offline scenario
            return "Offline"
```

---

#### Step 2: Periodic Status Checks (Background Job)

**What Happens**:
1. External system runs background job (every 5-15 minutes)
2. System calls SYNFLOX API to check license status
3. If status changed to Expired or Suspended:
   - System blocks access immediately
   - System shows notification to user
   - System logs status change
4. If status is Active:
   - System continues normal operation

**Implementation**:
- **Windows Service**: Background service checks every 10 minutes
- **Web Application**: Background job (Hangfire, Quartz.NET) checks every 15 minutes
- **Desktop Application**: Timer checks every 5 minutes

**Code Example (C# Background Service)**:
```csharp
public class LicenseStatusMonitor : BackgroundService
{
    private readonly LicenseChecker _licenseChecker;
    private readonly ILogger<LicenseStatusMonitor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = await _licenseChecker.CheckStatusAsync();
                
                if (status != LicenseStatus.Active)
                {
                    _logger.LogWarning("License status changed: {Status}", status);
                    // Block access, show notification
                    BlockApplicationAccess(status);
                }
                
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking license status");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

---

#### Step 3: Pre-Action Status Check (Before Critical Operations)

**What Happens**:
1. User attempts critical operation (export data, generate report, etc.)
2. System checks license status before allowing operation
3. If Active: Allow operation
4. If Expired/Suspended: Block operation, show message

**Code Example (C#)**:
```csharp
public async Task<bool> CanPerformOperationAsync()
{
    var status = await _licenseChecker.CheckStatusAsync();
    
    if (status == LicenseStatus.Active)
    {
        return true;
    }
    
    ShowLicenseError(status);
    return false;
}

public async Task ExportDataAsync()
{
    if (!await CanPerformOperationAsync())
    {
        return; // Blocked
    }
    
    // Perform export
    await _dataExportService.ExportAsync();
}
```

---

### Flow 2: Offline Licensing - License Key Validation

#### Step 1: Admin Generates License Key

**What Happens** (Admin Side):
1. Admin logs into SYNFLOX Admin Panel
2. Admin views company details
3. Admin clicks "Generate License Key"
4. SYNFLOX generates encrypted license key:
   - Creates payload: `{ CompanyId, ExpiryDate, IssuedDate, Version }`
   - Signs with HMAC SHA256
   - Encrypts with AES-256
   - Base64 encodes
5. Admin copies license key (shown only once)
6. Admin provides key to external system developer

**License Key Format**:
```
Base64(AES-256_Encrypt(JSON({
    Payload: {
        CompanyId: "550e8400-e29b-41d4-a716-446655440000",
        ExpiryDate: "2025-12-31T23:59:59Z",
        IssuedDate: "2025-01-15T10:00:00Z",
        Version: 1
    },
    Signature: "HMAC_SHA256(payload, signing_key)"
})))
```

**Example License Key**:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJDb21wYW55SWQiOiI1NTBlODQwMC1lMjliLTQxZDQtYTcxNi00NDY2NTU0NDAwMDAiLCJFeHBpcnlEYXRlIjoiMjAyNS0xMi0zMVQyMzo1OTo1OVoiLCJJc3N1ZWREYXRlIjoiMjAyNS0wMS0xNVQxMDowMDowMFoiLCJWZXJzaW9uIjoxfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

---

#### Step 2: System Validates License Key (Offline)

**What Happens**:
1. External system reads license key from configuration file
2. System decrypts license key:
   - Base64 decodes
   - AES-256 decrypts
   - Extracts payload and signature
3. System validates signature:
   - Computes HMAC SHA256 of payload
   - Compares with signature
4. System checks expiry date:
   - If `ExpiryDate < Now`: License expired
   - If `ExpiryDate >= Now`: License valid
5. System checks clock tampering:
   - If `IssuedDate > Now`: Clock tampering detected
   - If `Now - IssuedDate > MaxAge`: License too old (optional)
6. System allows or blocks access

**API Endpoint** (Optional - for online validation): `POST /api/licensing/validate-key`

**Request**:
```json
{
  "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (Valid)**:
```json
{
  "statusCode": 200,
  "message": "License key is valid",
  "data": {
    "isValid": true,
    "companyId": "encrypted_company_id",
    "expiryDate": "2025-12-31T23:59:59Z",
    "issuedDate": "2025-01-15T10:00:00Z",
    "isExpired": false,
    "clockTamperingDetected": false
  }
}
```

**Response (Expired)**:
```json
{
  "statusCode": 200,
  "message": "License key has expired",
  "data": {
    "isValid": false,
    "isExpired": true,
    "expiryDate": "2024-01-01T00:00:00Z",
    "clockTamperingDetected": false,
    "errorMessage": "License key expired on 2024-01-01"
  }
}
```

**Response (Clock Tampering)**:
```json
{
  "statusCode": 200,
  "message": "Clock tampering detected",
  "data": {
    "isValid": false,
    "isExpired": false,
    "clockTamperingDetected": true,
    "errorMessage": "System clock appears to have been tampered with"
  }
}
```

**Code Example (C# - Offline Validation)**:
```csharp
public class LicenseKeyValidator
{
    private readonly string _encryptionKey;
    private readonly string _signingKey;
    private readonly ILogger<LicenseKeyValidator> _logger;

    public LicenseKeyValidator(string encryptionKey, string signingKey)
    {
        _encryptionKey = encryptionKey;
        _signingKey = signingKey;
    }

    public ValidationResult Validate(string licenseKey)
    {
        try
        {
            // Step 1: Base64 decode
            var encryptedBytes = Convert.FromBase64String(licenseKey);
            
            // Step 2: AES-256 decrypt
            var decryptedJson = DecryptAES256(encryptedBytes, _encryptionKey);
            var token = JsonSerializer.Deserialize<LicenseToken>(decryptedJson);
            
            // Step 3: Validate HMAC signature
            var payloadJson = JsonSerializer.Serialize(token.Payload);
            var computedSignature = ComputeHMAC(payloadJson, _signingKey);
            
            if (computedSignature != token.Signature)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid license key signature"
                };
            }
            
            // Step 4: Check expiry date
            var now = DateTime.UtcNow;
            if (token.Payload.ExpiryDate < now)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    IsExpired = true,
                    ErrorMessage = $"License expired on {token.Payload.ExpiryDate:yyyy-MM-dd}"
                };
            }
            
            // Step 5: Check clock tampering
            if (token.Payload.IssuedDate > now)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ClockTamperingDetected = true,
                    ErrorMessage = "System clock appears to have been tampered with"
                };
            }
            
            // Step 6: License is valid
            return new ValidationResult
            {
                IsValid = true,
                CompanyId = token.Payload.CompanyId,
                ExpiryDate = token.Payload.ExpiryDate,
                IssuedDate = token.Payload.IssuedDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid license key format"
            };
        }
    }
    
    private string DecryptAES256(byte[] encrypted, string key)
    {
        // AES-256 decryption implementation
        // Use System.Security.Cryptography.Aes
    }
    
    private string ComputeHMAC(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
```

**Code Example (Python - Offline Validation)**:
```python
import base64
import json
import hmac
import hashlib
from datetime import datetime
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.backends import default_backend

class LicenseKeyValidator:
    def __init__(self, encryption_key: bytes, signing_key: bytes):
        self.encryption_key = encryption_key
        self.signing_key = signing_key

    def validate(self, license_key: str) -> dict:
        try:
            # Step 1: Base64 decode
            encrypted_bytes = base64.b64decode(license_key)
            
            # Step 2: AES-256 decrypt
            decrypted_json = self._decrypt_aes256(encrypted_bytes)
            token = json.loads(decrypted_json)
            
            # Step 3: Validate HMAC signature
            payload_json = json.dumps(token["Payload"], sort_keys=True)
            computed_signature = self._compute_hmac(payload_json)
            
            if computed_signature != token["Signature"]:
                return {
                    "isValid": False,
                    "errorMessage": "Invalid license key signature"
                }
            
            # Step 4: Check expiry date
            now = datetime.utcnow()
            expiry_date = datetime.fromisoformat(token["Payload"]["ExpiryDate"].replace("Z", "+00:00"))
            
            if expiry_date < now:
                return {
                    "isValid": False,
                    "isExpired": True,
                    "errorMessage": f"License expired on {expiry_date.date()}"
                }
            
            # Step 5: Check clock tampering
            issued_date = datetime.fromisoformat(token["Payload"]["IssuedDate"].replace("Z", "+00:00"))
            if issued_date > now:
                return {
                    "isValid": False,
                    "clockTamperingDetected": True,
                    "errorMessage": "System clock appears to have been tampered with"
                }
            
            # Step 6: License is valid
            return {
                "isValid": True,
                "companyId": token["Payload"]["CompanyId"],
                "expiryDate": token["Payload"]["ExpiryDate"],
                "issuedDate": token["Payload"]["IssuedDate"]
            }
            
        except Exception as e:
            return {
                "isValid": False,
                "errorMessage": f"Invalid license key format: {str(e)}"
            }
    
    def _decrypt_aes256(self, encrypted: bytes) -> str:
        # AES-256 decryption implementation
        pass
    
    def _compute_hmac(self, data: str) -> str:
        signature = hmac.new(
            self.signing_key,
            data.encode('utf-8'),
            hashlib.sha256
        ).digest()
        return base64.b64encode(signature).decode('utf-8')
```

---

#### Step 3: System Startup with License Key

**What Happens**:
1. External system starts up
2. System reads license key from configuration
3. System validates license key (offline)
4. If valid: Allow access, show expiry date
5. If expired: Block access, show "License Expired" message
6. If clock tampering: Block access, show "System Clock Error" message

**Code Example (C#)**:
```csharp
public class ApplicationStartup
{
    private readonly LicenseKeyValidator _validator;
    private readonly string _licenseKey;

    public bool Initialize()
    {
        // Read license key from config
        _licenseKey = ConfigurationManager.AppSettings["LicenseKey"];
        
        // Validate license key
        var result = _validator.Validate(_licenseKey);
        
        if (!result.IsValid)
        {
            if (result.IsExpired)
            {
                MessageBox.Show(
                    $"Your license expired on {result.ExpiryDate:yyyy-MM-dd}. " +
                    "Please contact your administrator to renew.",
                    "License Expired",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
            
            if (result.ClockTamperingDetected)
            {
                MessageBox.Show(
                    "System clock appears to have been tampered with. " +
                    "Please correct your system date and time.",
                    "Clock Tampering Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
            
            MessageBox.Show(
                result.ErrorMessage,
                "License Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return false;
        }
        
        // License is valid - show expiry warning if close
        var daysUntilExpiry = (result.ExpiryDate - DateTime.UtcNow).Days;
        if (daysUntilExpiry <= 30)
        {
            MessageBox.Show(
                $"Your license expires in {daysUntilExpiry} days. " +
                "Please contact your administrator to renew.",
                "License Expiring Soon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
        
        return true;
    }
}
```

---

### Flow 3: Hybrid Licensing (Online + Offline Fallback)

#### Step 1: Try Online First

**What Happens**:
1. System tries online status check first
2. If successful: Use online status
3. If failed (network error): Fallback to license key validation

**Code Example (C#)**:
```csharp
public class HybridLicenseChecker
{
    private readonly LicenseChecker _onlineChecker;
    private readonly LicenseKeyValidator _offlineValidator;
    private readonly string _licenseKey;

    public async Task<LicenseStatus> CheckStatusAsync()
    {
        // Try online first
        try
        {
            var status = await _onlineChecker.CheckStatusAsync();
            if (status != LicenseStatus.Offline)
            {
                return status; // Online check succeeded
            }
        }
        catch (Exception ex)
        {
            // Network error - fallback to offline
            _logger.LogWarning(ex, "Online check failed, falling back to license key validation");
        }
        
        // Fallback to offline license key validation
        var result = _offlineValidator.Validate(_licenseKey);
        
        if (!result.IsValid)
        {
            if (result.IsExpired)
                return LicenseStatus.Expired;
            if (result.ClockTamperingDetected)
                return LicenseStatus.Invalid;
            return LicenseStatus.Unknown;
        }
        
        return LicenseStatus.Active;
    }
}
```

---

### Flow 4: Webhook Integration

#### Step 1: Register Webhook

**What Happens** (Admin Side):
1. Admin logs into SYNFLOX Admin Panel
2. Admin views company details
3. Admin clicks "Add Webhook"
4. Admin enters:
   - URL: `https://your-system.com/webhooks/synflox`
   - Secret: `your_webhook_secret_key`
   - Events: Select events (Activated, Suspended, Expired, etc.)
5. Admin saves webhook
6. SYNFLOX stores webhook configuration

**Webhook Events**:
- `CompanyActivated` - Subscription activated
- `CompanySuspended` - Subscription suspended
- `CompanyResumed` - Subscription resumed
- `CompanyExpired` - Subscription expired
- `CompanyExtended` - Subscription extended
- `TrialStarted` - Trial period started
- `TrialExpired` - Trial period expired
- `TrialConverted` - Trial converted to paid

---

#### Step 2: Receive Webhook (External System)

**What Happens**:
1. SYNFLOX triggers webhook (when event occurs)
2. SYNFLOX POSTs to your webhook URL:
   ```http
   POST https://your-system.com/webhooks/synflox
   X-Webhook-Signature: {hmac_signature}
   X-Webhook-Event: CompanyExpired
   Content-Type: application/json
   
   {
     "eventType": "CompanyExpired",
     "companyId": "encrypted_company_id",
     "timestamp": "2025-01-15T10:00:00Z",
     "data": {
       "companyName": "Acme Corporation",
       "expiryDate": "2025-01-15T00:00:00Z",
       "previousStatus": "Active",
       "currentStatus": "Expired"
     }
   }
   ```
3. External system receives webhook
4. External system validates HMAC signature
5. External system processes event:
   - Update local license status
   - Block access if expired/suspended
   - Show notification to users
   - Log event

**HMAC Signature Validation**:
```csharp
public bool ValidateWebhookSignature(string payload, string signature, string secret)
{
    var computedSignature = ComputeHMAC(payload, secret);
    return computedSignature == signature;
}

private string ComputeHMAC(string data, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    return Convert.ToBase64String(hash);
}
```

**Webhook Endpoint Implementation (C#)**:
```csharp
[ApiController]
[Route("webhooks/synflox")]
public class SynfloxWebhookController : ControllerBase
{
    private readonly ILogger<SynfloxWebhookController> _logger;
    private readonly string _webhookSecret = "your_webhook_secret_key";

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()
    {
        try
        {
            // Read request body
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            
            // Get signature from header
            var signature = Request.Headers["X-Webhook-Signature"].ToString();
            var eventType = Request.Headers["X-Webhook-Event"].ToString();
            
            // Validate signature
            if (!ValidateSignature(body, signature, _webhookSecret))
            {
                _logger.LogWarning("Invalid webhook signature");
                return Unauthorized();
            }
            
            // Parse payload
            var payload = JsonSerializer.Deserialize<WebhookPayload>(body);
            
            // Process event
            await ProcessWebhookEvent(payload, eventType);
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }
    
    private async Task ProcessWebhookEvent(WebhookPayload payload, string eventType)
    {
        switch (eventType)
        {
            case "CompanyExpired":
                await HandleExpired(payload);
                break;
            case "CompanySuspended":
                await HandleSuspended(payload);
                break;
            case "CompanyActivated":
                await HandleActivated(payload);
                break;
            // ... other events
        }
    }
    
    private async Task HandleExpired(WebhookPayload payload)
    {
        _logger.LogInformation("Company expired: {CompanyId}", payload.CompanyId);
        
        // Update local license status
        await _licenseService.UpdateStatusAsync(
            payload.CompanyId,
            LicenseStatus.Expired
        );
        
        // Block access
        await _accessControlService.BlockAccessAsync();
        
        // Show notification to users
        await _notificationService.ShowExpiredNotificationAsync();
    }
}
```

**Webhook Endpoint Implementation (Python/Flask)**:
```python
from flask import Flask, request, jsonify
import hmac
import hashlib
import base64
import json

app = Flask(__name__)
WEBHOOK_SECRET = "your_webhook_secret_key"

@app.route('/webhooks/synflox', methods=['POST'])
def receive_webhook():
    try:
        # Read request body
        body = request.get_data(as_text=True)
        
        # Get signature from header
        signature = request.headers.get('X-Webhook-Signature')
        event_type = request.headers.get('X-Webhook-Event')
        
        # Validate signature
        if not validate_signature(body, signature, WEBHOOK_SECRET):
            return jsonify({"error": "Invalid signature"}), 401
        
        # Parse payload
        payload = json.loads(body)
        
        # Process event
        process_webhook_event(payload, event_type)
        
        return jsonify({"status": "ok"}), 200
        
    except Exception as e:
        return jsonify({"error": str(e)}), 500

def validate_signature(payload: str, signature: str, secret: str) -> bool:
    computed = hmac.new(
        secret.encode('utf-8'),
        payload.encode('utf-8'),
        hashlib.sha256
    ).digest()
    computed_b64 = base64.b64encode(computed).decode('utf-8')
    return computed_b64 == signature

def process_webhook_event(payload: dict, event_type: str):
    if event_type == "CompanyExpired":
        handle_expired(payload)
    elif event_type == "CompanySuspended":
        handle_suspended(payload)
    # ... other events

def handle_expired(payload: dict):
    company_id = payload["companyId"]
    print(f"Company expired: {company_id}")
    # Update local license status
    # Block access
    # Show notification
```

---

#### Step 3: Handle Webhook Events

**Event: CompanyExpired**

**Action**: Block access immediately

**Code**:
```csharp
private async Task HandleExpired(WebhookPayload payload)
{
    // Update status
    await _licenseService.UpdateStatusAsync(LicenseStatus.Expired);
    
    // Block all access
    await _accessControlService.BlockAllAccessAsync();
    
    // Show message to users
    await _notificationService.ShowMessageAsync(
        "Your subscription has expired. Please contact your administrator."
    );
    
    // Log event
    _logger.LogWarning("License expired for company: {CompanyId}", payload.CompanyId);
}
```

**Event: CompanySuspended**

**Action**: Block access, show suspension message

**Code**:
```csharp
private async Task HandleSuspended(WebhookPayload payload)
{
    await _licenseService.UpdateStatusAsync(LicenseStatus.Suspended);
    await _accessControlService.BlockAllAccessAsync();
    await _notificationService.ShowMessageAsync(
        "Your subscription has been suspended. Please contact support."
    );
}
```

**Event: CompanyActivated**

**Action**: Allow access, show welcome message

**Code**:
```csharp
private async Task HandleActivated(WebhookPayload payload)
{
    await _licenseService.UpdateStatusAsync(LicenseStatus.Active);
    await _accessControlService.AllowAccessAsync();
    await _notificationService.ShowMessageAsync(
        "Your subscription has been activated. Welcome back!"
    );
}
```

---

### Flow 5: API Key Management

#### Step 1: Get API Key from Admin

**What Happens**:
1. Admin creates API key in SYNFLOX Admin Panel
2. Admin copies API key (shown only once)
3. Admin provides key to external system developer
4. Developer stores key securely

**Storage Options**:
- Environment variables (recommended)
- Secure vault (Azure Key Vault, AWS Secrets Manager)
- Encrypted configuration file
- **Never** hardcode in source code

**Example (Environment Variable)**:
```bash
# .env file (never commit to git)
SYNFLOX_API_KEY=sk_live_abc123xyz789...
SYNFLOX_COMPANY_ID=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Example (C# Configuration)**:
```csharp
// appsettings.json (never commit with real keys)
{
  "Synflox": {
    "ApiKey": "${SYNFLOX_API_KEY}", // Use environment variable
    "CompanyId": "${SYNFLOX_COMPANY_ID}",
    "BaseUrl": "https://api.synflox.com"
  }
}
```

---

#### Step 2: Use API Key in Requests

**What Happens**:
1. External system makes API request
2. System adds `Authorization: Bearer {api_key}` header
3. SYNFLOX validates API key:
   - Checks key exists and is active
   - Checks expiry date (if set)
   - Checks IP whitelist (if configured)
   - Checks rate limit
4. If valid: Process request
5. If invalid: Return 401 Unauthorized

**Code Example**:
```csharp
var apiKey = Environment.GetEnvironmentVariable("SYNFLOX_API_KEY");
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

var response = await httpClient.GetAsync("https://api.synflox.com/api/licensing/{id}/status");
```

---

#### Step 3: Handle API Key Errors

**401 Unauthorized**:
```json
{
  "statusCode": 401,
  "message": "Invalid or expired API key",
  "data": null
}
```

**Action**: Contact admin to regenerate API key

**429 Too Many Requests** (Rate Limit):
```json
{
  "statusCode": 429,
  "message": "Rate limit exceeded. Please try again later.",
  "data": null
}
```

**Action**: Wait and retry, or implement exponential backoff

---

### Flow 6: HMAC Request Signing (Enhanced Security)

#### Step 1: Get Signing Secret

**What Happens**:
1. Admin creates API key in SYNFLOX Admin Panel
2. SYNFLOX generates signing secret (separate from API key)
3. Admin copies signing secret (shown only once)
4. Admin provides secret to external system developer

**Note**: Signing secret is different from API key. Both are needed for HMAC signing.

---

#### Step 2: Sign Request

**What Happens**:
1. External system prepares request
2. System creates signature:
   ```
   timestamp = UnixTimestamp()
   message = timestamp + method + path + body
   signature = HMAC-SHA256(message, signing_secret)
   ```
3. System adds headers:
   - `X-Signature: {signature}`
   - `X-Timestamp: {timestamp}`
4. System sends request

**Code Example (C#)**:
```csharp
public class HmacRequestSigner
{
    private readonly string _signingSecret;

    public HttpRequestMessage SignRequest(HttpRequestMessage request, string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var method = request.Method.Method;
        var path = request.RequestUri.AbsolutePath;
        
        var message = $"{timestamp}{method}{path}{body}";
        var signature = ComputeHMAC(message, _signingSecret);
        
        request.Headers.Add("X-Signature", signature);
        request.Headers.Add("X-Timestamp", timestamp);
        
        return request;
    }
    
    private string ComputeHMAC(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
```

**Code Example (Python)**:
```python
import hmac
import hashlib
import base64
import time

class HmacRequestSigner:
    def __init__(self, signing_secret: str):
        self.signing_secret = signing_secret

    def sign_request(self, method: str, path: str, body: str) -> dict:
        timestamp = str(int(time.time()))
        message = f"{timestamp}{method}{path}{body}"
        
        signature = hmac.new(
            self.signing_secret.encode('utf-8'),
            message.encode('utf-8'),
            hashlib.sha256
        ).digest()
        
        signature_b64 = base64.b64encode(signature).decode('utf-8')
        
        return {
            "X-Signature": signature_b64,
            "X-Timestamp": timestamp
        }
```

---

#### Step 3: SYNFLOX Validates Signature

**What Happens**:
1. SYNFLOX receives request
2. SYNFLOX extracts `X-Signature` and `X-Timestamp` headers
3. SYNFLOX validates timestamp (within 5 minutes)
4. SYNFLOX computes signature:
   ```
   message = timestamp + method + path + body
   computed_signature = HMAC-SHA256(message, stored_signing_secret)
   ```
5. SYNFLOX compares signatures
6. If match: Process request
7. If mismatch: Return 401 Unauthorized

---

## 🔄 Complete Integration Scenarios

### Scenario 1: ERP System Integration

**Requirements**:
- Check license status on startup
- Check license status before critical operations
- Receive webhooks for status changes
- Block access if expired/suspended

**Implementation**:
```csharp
public class ErpLicenseManager
{
    private readonly LicenseChecker _checker;
    private readonly LicenseKeyValidator _validator;
    private readonly WebhookReceiver _webhookReceiver;

    public async Task<bool> InitializeAsync()
    {
        // Check status on startup
        var status = await _checker.CheckStatusAsync();
        if (status != LicenseStatus.Active)
        {
            ShowLicenseError(status);
            return false;
        }
        
        // Start webhook receiver
        _webhookReceiver.Start();
        
        return true;
    }
    
    public async Task<bool> CanExportDataAsync()
    {
        var status = await _checker.CheckStatusAsync();
        return status == LicenseStatus.Active;
    }
}
```

---

### Scenario 2: POS System Integration

**Requirements**:
- Offline license key validation (no internet in store)
- Clock tampering detection
- Periodic validation (every hour)

**Implementation**:
```csharp
public class PosLicenseManager
{
    private readonly LicenseKeyValidator _validator;
    private readonly Timer _validationTimer;

    public bool Initialize()
    {
        // Validate license key on startup
        var result = _validator.Validate(GetLicenseKey());
        if (!result.IsValid)
        {
            ShowLicenseError(result);
            return false;
        }
        
        // Validate every hour
        _validationTimer = new Timer(ValidateLicense, null, 
            TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        
        return true;
    }
    
    private void ValidateLicense(object state)
    {
        var result = _validator.Validate(GetLicenseKey());
        if (!result.IsValid)
        {
            BlockApplication();
        }
    }
}
```

---

### Scenario 3: Web Application Integration

**Requirements**:
- Real-time status checks
- Webhook integration
- User-friendly error messages
- Graceful degradation

**Implementation**:
```csharp
public class WebAppLicenseManager
{
    private readonly LicenseChecker _checker;
    private LicenseStatus _cachedStatus;
    private DateTime _lastCheck;

    public async Task<bool> CheckAccessAsync()
    {
        // Cache status for 5 minutes
        if (DateTime.UtcNow - _lastCheck > TimeSpan.FromMinutes(5))
        {
            try
            {
                _cachedStatus = await _checker.CheckStatusAsync();
                _lastCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                // Graceful degradation - use cached status
                _logger.LogWarning(ex, "Failed to check license status, using cached");
            }
        }
        
        return _cachedStatus == LicenseStatus.Active;
    }
}
```

---

## 🚨 Error Handling

### Network Errors

**Scenario**: API call fails (network timeout, DNS error)

**Handling**:
- **Online-Only Systems**: Show error, retry with exponential backoff
- **Hybrid Systems**: Fallback to license key validation
- **Offline Systems**: Use cached status or license key

**Code**:
```csharp
public async Task<LicenseStatus> CheckStatusWithRetryAsync()
{
    var maxRetries = 3;
    var delay = TimeSpan.FromSeconds(1);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await _checker.CheckStatusAsync();
        }
        catch (HttpRequestException ex)
        {
            if (i == maxRetries - 1)
            {
                // Last retry failed - fallback to license key
                return await ValidateLicenseKeyAsync();
            }
            
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
        }
    }
    
    return LicenseStatus.Unknown;
}
```

---

### Rate Limiting Errors

**Scenario**: Too many API requests (429 Too Many Requests)

**Handling**:
- Wait and retry
- Implement request throttling
- Cache status for longer periods

**Code**:
```csharp
public async Task<LicenseStatus> CheckStatusWithRateLimitAsync()
{
    try
    {
        return await _checker.CheckStatusAsync();
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("429"))
    {
        // Rate limited - wait 1 minute and retry
        await Task.Delay(TimeSpan.FromMinutes(1));
        return await _checker.CheckStatusAsync();
    }
}
```

---

### Invalid License Key

**Scenario**: License key is corrupted or invalid

**Handling**:
- Show clear error message
- Log error for debugging
- Contact admin for new license key

**Code**:
```csharp
public ValidationResult ValidateLicenseKey(string licenseKey)
{
    try
    {
        return _validator.Validate(licenseKey);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating license key");
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = "Invalid license key format. Please contact your administrator."
        };
    }
}
```

---

## 📊 Status Codes Reference

### License Status Values

| Status | Meaning | Action |
|--------|---------|--------|
| `Active` | Subscription is active | Allow access |
| `Expired` | Subscription has expired | Block access, show renewal message |
| `Suspended` | Subscription is suspended | Block access, show suspension message |
| `Unknown` | Status check failed | Use fallback (license key) or show error |
| `Offline` | Network error | Fallback to license key validation |

---

## 🔒 Security Best Practices

### 1. Secure API Key Storage

**✅ DO**:
- Store in environment variables
- Use secure vaults (Azure Key Vault, AWS Secrets Manager)
- Encrypt configuration files
- Rotate keys regularly

**❌ DON'T**:
- Hardcode in source code
- Commit to version control
- Store in plain text files
- Share keys publicly

---

### 2. HMAC Request Signing

**✅ DO**:
- Sign all requests with HMAC
- Validate timestamps (prevent replay attacks)
- Use strong signing secrets
- Rotate secrets regularly

---

### 3. License Key Security

**✅ DO**:
- Store license keys securely
- Validate keys on every startup
- Check for clock tampering
- Log validation failures

**❌ DON'T**:
- Store keys in plain text
- Skip validation
- Ignore clock tampering warnings

---

### 4. Webhook Security

**✅ DO**:
- Validate HMAC signatures
- Use HTTPS for webhook endpoints
- Validate event types
- Log all webhook events

**❌ DON'T**:
- Skip signature validation
- Use HTTP (unencrypted)
- Trust unverified events

---

## 📝 Configuration Examples

### C# (.NET) Configuration

**appsettings.json**:
```json
{
  "Synflox": {
    "BaseUrl": "https://api.synflox.com",
    "CompanyId": "${SYNFLOX_COMPANY_ID}",
    "ApiKey": "${SYNFLOX_API_KEY}",
    "SigningSecret": "${SYNFLOX_SIGNING_SECRET}",
    "LicenseKey": "${SYNFLOX_LICENSE_KEY}",
    "CheckIntervalMinutes": 10,
    "EnableWebhooks": true,
    "WebhookSecret": "${SYNFLOX_WEBHOOK_SECRET}"
  }
}
```

**Startup.cs**:
```csharp
services.Configure<SynfloxSettings>(
    Configuration.GetSection("Synflox")
);

services.AddSingleton<LicenseChecker>();
services.AddSingleton<LicenseKeyValidator>();
services.AddHostedService<LicenseStatusMonitor>();
```

---

### Python Configuration

**config.py**:
```python
import os

SYNFLOX_CONFIG = {
    "base_url": "https://api.synflox.com",
    "company_id": os.getenv("SYNFLOX_COMPANY_ID"),
    "api_key": os.getenv("SYNFLOX_API_KEY"),
    "signing_secret": os.getenv("SYNFLOX_SIGNING_SECRET"),
    "license_key": os.getenv("SYNFLOX_LICENSE_KEY"),
    "check_interval_minutes": 10,
    "enable_webhooks": True,
    "webhook_secret": os.getenv("SYNFLOX_WEBHOOK_SECRET")
}
```

---

### JavaScript/Node.js Configuration

**config.js**:
```javascript
module.exports = {
    synflox: {
        baseUrl: process.env.SYNFLOX_BASE_URL || 'https://api.synflox.com',
        companyId: process.env.SYNFLOX_COMPANY_ID,
        apiKey: process.env.SYNFLOX_API_KEY,
        signingSecret: process.env.SYNFLOX_SIGNING_SECRET,
        licenseKey: process.env.SYNFLOX_LICENSE_KEY,
        checkIntervalMinutes: 10,
        enableWebhooks: process.env.SYNFLOX_ENABLE_WEBHOOKS === 'true',
        webhookSecret: process.env.SYNFLOX_WEBHOOK_SECRET
    }
};
```

---

## 🎯 Integration Checklist

### Pre-Integration

- [ ] Get company ID from admin
- [ ] Get API key from admin (if using online licensing)
- [ ] Get license key from admin (if using offline licensing)
- [ ] Get webhook secret from admin (if using webhooks)
- [ ] Get signing secret from admin (if using HMAC signing)
- [ ] Configure secure storage for all secrets

### Integration Steps

- [ ] Implement license status check
- [ ] Implement license key validation (if offline)
- [ ] Implement webhook receiver (if using webhooks)
- [ ] Implement HMAC request signing (if using)
- [ ] Add error handling
- [ ] Add logging
- [ ] Add user notifications
- [ ] Test all scenarios (Active, Expired, Suspended)
- [ ] Test network error handling
- [ ] Test rate limiting
- [ ] Test webhook delivery

### Post-Integration

- [ ] Monitor license checks
- [ ] Monitor webhook deliveries
- [ ] Review error logs
- [ ] Update documentation
- [ ] Train support team

---

## 📚 API Reference Summary

### Status Check Endpoint

**Endpoint**: `GET /api/licensing/{encrypted_company_id}/status`

**Authentication**: Optional (public endpoint)

**Response**: `{ status: "Active" | "Expired" | "Suspended" }`

---

### License Key Validation Endpoint

**Endpoint**: `POST /api/licensing/validate-key`

**Authentication**: Optional (public endpoint)

**Request**: `{ "licenseKey": "..." }`

**Response**: `{ isValid: true/false, isExpired: true/false, clockTamperingDetected: true/false }`

---

### Webhook Events

**URL**: Your webhook endpoint (configured in admin panel)

**Method**: POST

**Headers**:
- `X-Webhook-Signature: {hmac_signature}`
- `X-Webhook-Event: {event_type}`

**Payload**: `{ eventType, companyId, timestamp, data }`

---

## 🎓 Complete Example: ERP System Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure SYNFLOX
builder.Services.Configure<SynfloxSettings>(
    builder.Configuration.GetSection("Synflox")
);

// Register services
builder.Services.AddSingleton<LicenseChecker>();
builder.Services.AddSingleton<LicenseKeyValidator>();
builder.Services.AddSingleton<WebhookReceiver>();
builder.Services.AddHostedService<LicenseStatusMonitor>();

var app = builder.Build();

// Startup license check
var licenseChecker = app.Services.GetRequiredService<LicenseChecker>();
var status = await licenseChecker.CheckStatusAsync();
if (status != LicenseStatus.Active)
{
    app.Logger.LogCritical("License is not active: {Status}", status);
    // Block application startup or show error
}

app.Run();
```

```csharp
// LicenseStatusMonitor.cs
public class LicenseStatusMonitor : BackgroundService
{
    private readonly LicenseChecker _checker;
    private readonly ILogger<LicenseStatusMonitor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = await _checker.CheckStatusAsync();
                
                if (status != LicenseStatus.Active)
                {
                    _logger.LogWarning("License status changed: {Status}", status);
                    // Block access, show notification
                }
                
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking license status");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

```csharp
// WebhookReceiver.cs
[ApiController]
[Route("webhooks/synflox")]
public class SynfloxWebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["X-Webhook-Signature"].ToString();
        
        if (!ValidateSignature(body, signature))
        {
            return Unauthorized();
        }
        
        var payload = JsonSerializer.Deserialize<WebhookPayload>(body);
        await ProcessEvent(payload);
        
        return Ok();
    }
}
```

---

**This guide covers all integration flows for external systems. Every scenario, every step, every code example is documented here.**

For admin panel flows, refer to `INTERNAL_ADMIN_FLOW_GUIDE.md`.  
For complete API documentation, refer to `README.md`.

