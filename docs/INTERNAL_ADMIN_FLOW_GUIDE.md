# SYNFLOX Internal Admin Panel - Complete Business Flow Guide

## 📋 Overview

This guide documents **every business flow** in the SYNFLOX Admin Panel - the internal management system used by administrators to manage companies, subscriptions, plans, and all system operations.

**Target Audience**: Frontend Developers, Business Analysts, System Administrators  
**Purpose**: Complete understanding of all business processes and workflows

---

## 🎯 System Purpose

SYNFLOX Admin Panel is the **central control center** where administrators:
- Manage companies (customers/tenants)
- Control subscription status (Active, Suspended, Expired)
- Manage subscription plans and pricing
- Configure projects and modules
- Monitor system usage and analytics
- Handle notifications and reports
- Manage API keys and webhooks

---

## 👥 User Roles

### SuperAdmin
- **Full System Access**: Can perform all operations
- **Can Manage**: Companies, Plans, Projects, Modules, Admins, Settings
- **Authorization**: `SuperAdminOnly` policy

### Admin
- **Limited Access**: Read-only for some features, can view dashboard
- **Can View**: Companies, Plans, Reports, Analytics
- **Authorization**: `AdminOrSuperAdmin` policy

---

## 🔄 Complete Business Flows

### Flow 1: New Company Onboarding (Complete Lifecycle)

#### Step 1: Admin Logs In

**What Happens**:
1. Admin navigates to login page
2. Enters username and password
3. System validates credentials
4. System checks failed login attempts (max 5, lockout 30 minutes)
5. System validates password against password policy
6. System generates JWT access token (30 minutes) and refresh token (60 days)
7. System records successful login attempt
8. Admin is redirected to dashboard

**API Endpoint**: `POST /api/admin/auth/login`

**Request**:
```json
{
  "username": "admin",
  "password": "SecurePassword123!"
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

**Business Rules**:
- Password must meet policy requirements (min 8 chars, uppercase, lowercase, numbers, special chars)
- Account locked after 5 failed attempts for 30 minutes
- Password expires after 90 days (configurable)
- Cannot reuse last 5 passwords (configurable)

---

#### Step 2: Admin Views Dashboard

**What Happens**:
1. Admin lands on dashboard
2. System fetches statistics: `GET /api/dashboard/statistics`
3. System displays:
   - Total Companies: 150
   - Active Companies: 120
   - Expired Companies: 20
   - Suspended Companies: 10
4. System shows charts:
   - Status distribution (donut chart)
   - Expiry timeline (area chart)
   - API usage (bar chart)
5. System shows recent notifications
6. System auto-refreshes every 5 minutes

**API Endpoint**: `GET /api/dashboard/statistics`

**Dashboard Data**:
- Real-time statistics
- Visual charts for insights
- Recent activity feed
- Quick action buttons

---

#### Step 3: Admin Creates New Company

**What Happens**:
1. Admin navigates to Companies page
2. Admin clicks "Create Company" button
3. Admin fills form:
   - Company Name: "Acme Corporation"
   - Contact Email: "admin@acme.com"
   - Contact Phone: "+1234567890"
   - Address: "123 Main St, City, Country"
   - Subscription Plan: (optional) Select from dropdown
   - Initial Expiry Date: (optional) Set future date
4. Admin clicks "Create"
5. System validates input (required fields, email format)
6. System creates Company entity:
   - `IsActive = false` (default - not activated yet)
   - `IsExpired = false` (default)
   - `IsTrial = false` (default)
   - `CreatedTimestamp = DateTime.UtcNow`
7. System saves to database
8. System logs to SubscriptionHistory:
   - `ActionType = Created`
   - `OldValue = null`
   - `NewValue = { company data JSON }`
   - `PerformedBy = current_admin_id`
   - `Timestamp = DateTime.UtcNow`
9. System encrypts company ID
10. System returns success response with company data

**API Endpoint**: `POST /api/companies`

**Request**:
```json
{
  "name": "Acme Corporation",
  "contactEmail": "admin@acme.com",
  "contactPhone": "+1234567890",
  "address": "123 Main St, City, Country",
  "subscriptionPlanId": "encrypted_plan_id",
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

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
    "subscriptionPlanId": "encrypted_plan_id",
    "isTrial": false,
    "trialEndDate": null,
    "createdAt": "2025-01-15T10:00:00Z"
  }
}
```

**Business Rules**:
- Company is created with `IsActive = false` (must be activated separately)
- Company name is required
- Email must be valid format
- Subscription plan is optional (can be assigned later)
- Expiry date is optional (can be set during activation)

**What Gets Logged**:
- SubscriptionHistory entry with `Created` action
- Full company data in `NewValue` JSON field
- Admin who created it in `PerformedBy`

---

#### Step 4: Admin Assigns Subscription Plan (Optional)

**What Happens**:
1. Admin views company details
2. Admin clicks "Assign Plan" or "Change Plan"
3. Admin selects subscription plan from dropdown
4. System updates company:
   - `SubscriptionPlanId = selected_plan_id`
   - `UpdatedTimestamp = DateTime.UtcNow`
5. System saves to database
6. System logs to SubscriptionHistory:
   - `ActionType = Updated`
   - `OldValue = { previous plan data }`
   - `NewValue = { new plan data }`
7. System returns updated company

**API Endpoint**: `PUT /api/companies/{id}`

**Request**:
```json
{
  "subscriptionPlanId": "encrypted_plan_id"
}
```

**Business Rules**:
- Plan must exist and be active
- Company can change plans at any time
- Plan change doesn't affect subscription status
- Plan determines which modules/features company can access

---

#### Step 5: Admin Starts Trial Period (Optional)

**What Happens**:
1. Admin views company details
2. Admin clicks "Start Trial" button
3. Admin enters trial duration (default: 30 days)
4. System validates:
   - Company is not already in trial
   - Company is not already active
5. System updates company:
   - `IsTrial = true`
   - `TrialEndDate = DateTime.UtcNow + trialDays`
   - `IsActive = true` (trial is active)
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System creates Notification:
   - `Type = Activated` (trial started)
   - `Title = "Trial Period Started"`
   - `Message = "Your 30-day trial has started"`
   - `CompanyId = company_id`
   - `IsRead = false`
8. System sends email (if `ContactEmail` exists):
   - Subject: "Trial Period Started"
   - Body: Localized email template
9. System triggers webhooks:
   - Finds active webhooks for company
   - Filters by event: `TrialStarted`
   - For each webhook:
     - Creates WebhookDelivery record
     - POSTs to webhook URL with HMAC signature
     - Logs delivery result
10. System logs to SubscriptionHistory:
    - `ActionType = TrialStarted`
    - `OldValue = { previous state }`
    - `NewValue = { new state with IsTrial = true }`
11. System returns updated company

**API Endpoint**: `POST /api/licensing/{id}/trial/start`

**Request**:
```json
{
  "trialDays": 30
}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "Trial period started successfully",
  "data": {
    "id": "encrypted_company_id",
    "name": "Acme Corporation",
    "isTrial": true,
    "trialEndDate": "2025-02-14T10:00:00Z",
    "isActive": true,
    ...
  }
}
```

**Business Rules**:
- Trial duration is configurable (default: 30 days)
- Company can only have one trial period
- Trial automatically expires when `TrialEndDate` passes
- Trial can be converted to paid subscription before expiry

**What Gets Created**:
- Notification (in-system)
- Email notification (if email exists)
- Webhook delivery (if webhooks configured)
- SubscriptionHistory entry

---

#### Step 6: Admin Activates Subscription

**What Happens**:
1. Admin views company details
2. Admin clicks "Activate Subscription" button
3. Admin enters expiry date (or uses default: 1 year from now)
4. System validates:
   - Expiry date must be in the future
   - Company exists and is not deleted
5. System updates company:
   - `IsActive = true`
   - `ExpiryDate = provided_expiry_date`
   - `IsExpired = false` (if was expired)
   - `IsTrial = false` (if was in trial)
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System creates Notification:
   - `Type = Activated`
   - `Title = "Subscription Activated"`
   - `Message = "Your subscription has been activated"`
   - `CompanyId = company_id`
8. System sends email (if `ContactEmail` exists):
   - Subject: "Subscription Activated"
   - Body: Localized email template with expiry date
9. System triggers webhooks:
   - Finds active webhooks for company
   - Filters by event: `CompanyActivated`
   - POSTs to webhook URLs with HMAC signature
   - Logs all delivery attempts
10. System logs to SubscriptionHistory:
    - `ActionType = Activated`
    - `OldValue = { previous state }`
    - `NewValue = { new state with IsActive = true, ExpiryDate }`
11. System returns updated company

**API Endpoint**: `PUT /api/licensing/{id}/activate`

**Request**:
```json
{
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

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
    "isExpired": false,
    "isTrial": false,
    ...
  }
}
```

**Business Rules**:
- Expiry date must be in the future
- Activation sets `IsActive = true` and `IsExpired = false`
- If company was in trial, trial is ended
- External systems can now check status and get "Active"

**What Gets Created**:
- Notification (in-system)
- Email notification (if email exists)
- Webhook delivery (if webhooks configured)
- SubscriptionHistory entry

**Status Calculation After Activation**:
- `IsActive = true`
- `ExpiryDate = 2025-12-31` (future date)
- `IsExpired = false`
- **Status = Active** ✅

---

#### Step 7: Admin Generates License Key (For Offline Systems)

**What Happens**:
1. Admin views company details
2. Admin clicks "Generate License Key" button
3. System validates:
   - Company exists and is active
   - Company has valid expiry date
4. System generates license key:
   - Creates payload JSON:
     ```json
     {
       "CompanyId": "550e8400-e29b-41d4-a716-446655440000",
       "ExpiryDate": "2025-12-31T23:59:59Z",
       "IssuedDate": "2025-01-15T10:00:00Z",
       "Version": 1
     }
     ```
   - Generates HMAC SHA256 signature: `HMAC_SHA256(payload, signing_key)`
   - Creates signed token: `{ Payload: {...}, Signature: "..." }`
   - Encrypts with AES-256: `AES_256_Encrypt(signed_token, encryption_key, iv)`
   - Base64 encodes: `Base64(encrypted_bytes)`
5. System updates company:
   - `LicenseKey = generated_key`
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System returns license key to admin
8. **IMPORTANT**: Key is shown only once - admin must copy it immediately

**API Endpoint**: `POST /api/licensing/{id}/license-key/generate`

**Response**:
```json
{
  "statusCode": 200,
  "message": "License key generated successfully",
  "data": {
    "licenseKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "message": "License key generated successfully. Store this key securely - it will not be shown again."
  }
}
```

**Business Rules**:
- License key can be regenerated (old key becomes invalid)
- Key contains all necessary information for offline validation
- Key is encrypted and signed for security
- Key includes clock tampering detection (IssuedDate)

**License Key Format**:
```
Base64(AES-256_Encrypt(JSON({
    Payload: {
        CompanyId: Guid,
        ExpiryDate: DateTime,
        IssuedDate: DateTime,
        Version: int
    },
    Signature: HMAC_SHA256(Payload, SigningKey)
})))
```

---

#### Step 8: External System Checks Status (Real-Time)

**What Happens** (External system perspective):
1. External system (ERP/CRM/POS) calls SYNFLOX API
2. System receives request: `GET /api/licensing/{encrypted_id}/status`
3. System decrypts company ID
4. System loads company from database
5. System calculates status:
   ```csharp
   if (company.IsExpired || company.ExpiryDate < Now)
       return LicenseStatus.Expired;
   if (!company.IsActive && company.ExpiryDate >= Now)
       return LicenseStatus.Suspended;
   if (company.IsActive && company.ExpiryDate >= Now)
       return LicenseStatus.Active;
   ```
6. System returns status response
7. External system allows or blocks access based on status

**API Endpoint**: `GET /api/licensing/{id}/status` (Public - no auth required)

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

**Business Rules**:
- Status check is public (no authentication required)
- Status is calculated in real-time
- Expired status takes priority over Active/Suspended
- Response is localized (English or Arabic based on Accept-Language header)

---

### Flow 2: Subscription Management Operations

#### Flow 2.1: Suspend Subscription

**What Happens**:
1. Admin views company details
2. Admin clicks "Suspend Subscription" button
3. Admin confirms action
4. System validates:
   - Company exists and is not deleted
   - Company is currently active
5. System updates company:
   - `IsActive = false`
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System creates Notification:
   - `Type = Suspended`
   - `Title = "Subscription Suspended"`
   - `Message = "Your subscription has been suspended"`
8. System sends email (if `ContactEmail` exists)
9. System triggers webhooks (EventType: `CompanySuspended`)
10. System logs to SubscriptionHistory:
    - `ActionType = Suspended`
    - `OldValue = { IsActive: true }`
    - `NewValue = { IsActive: false }`
11. System returns updated company

**API Endpoint**: `PUT /api/licensing/{id}/suspend`

**Business Rules**:
- Suspension is immediate
- External systems will get "Suspended" status on next check
- Suspension doesn't change expiry date
- Can be resumed later

**Status After Suspension**:
- `IsActive = false`
- `ExpiryDate = 2025-12-31` (unchanged)
- `IsExpired = false`
- **Status = Suspended** ⏸️

---

#### Flow 2.2: Resume Subscription

**What Happens**:
1. Admin views company details
2. Admin clicks "Resume Subscription" button
3. Admin confirms action
4. System validates:
   - Company exists and is not deleted
   - Company is currently suspended
   - Expiry date is in the future (or set new expiry)
5. System updates company:
   - `IsActive = true`
   - `IsExpired = false` (if was expired)
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System creates Notification:
   - `Type = Resumed`
   - `Title = "Subscription Resumed"`
   - `Message = "Your subscription has been resumed"`
8. System sends email (if `ContactEmail` exists)
9. System triggers webhooks (EventType: `CompanyResumed`)
10. System logs to SubscriptionHistory:
    - `ActionType = Resumed`
    - `OldValue = { IsActive: false }`
    - `NewValue = { IsActive: true }`
11. System returns updated company

**API Endpoint**: `PUT /api/licensing/{id}/resume`

**Business Rules**:
- Resume is immediate
- External systems will get "Active" status on next check
- If expiry date passed, must set new expiry date

**Status After Resume**:
- `IsActive = true`
- `ExpiryDate = 2025-12-31` (future date)
- `IsExpired = false`
- **Status = Active** ✅

---

#### Flow 2.3: Extend Subscription

**What Happens**:
1. Admin views company details
2. Admin clicks "Extend Subscription" button
3. Admin enters new expiry date
4. System validates:
   - New expiry date must be after current expiry date
   - Company exists and is not deleted
5. System updates company:
   - `ExpiryDate = new_expiry_date`
   - `IsExpired = false` (if was expired)
   - `IsActive = true` (if was expired)
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System creates Notification:
   - `Type = Extended`
   - `Title = "Subscription Extended"`
   - `Message = "Your subscription has been extended until {new_expiry_date}"`
8. System sends email (if `ContactEmail` exists)
9. System triggers webhooks (EventType: `CompanyExtended`)
10. System logs to SubscriptionHistory:
    - `ActionType = Extended`
    - `OldValue = { ExpiryDate: "2025-01-01" }`
    - `NewValue = { ExpiryDate: "2026-01-01" }`
11. System returns updated company

**API Endpoint**: `PUT /api/licensing/{id}/extend`

**Request**:
```json
{
  "newExpiryDate": "2026-12-31T23:59:59Z"
}
```

**Business Rules**:
- New expiry date must be after current expiry date
- Extension reactivates expired subscriptions
- Extension doesn't change `IsActive` if already active

**Status After Extension**:
- `IsActive = true` (if was expired, now active)
- `ExpiryDate = 2026-12-31` (new date)
- `IsExpired = false`
- **Status = Active** ✅

---

#### Flow 2.4: Convert Trial to Paid Subscription

**What Happens**:
1. Admin views company details (currently in trial)
2. Admin clicks "Convert Trial to Paid" button
3. Admin enters expiry date for paid subscription
4. System validates:
   - Company is currently in trial
   - Expiry date is in the future
5. System updates company:
   - `IsTrial = false`
   - `IsActive = true`
   - `ExpiryDate = provided_expiry_date`
   - `TrialEndDate = null` (clear trial date)
   - `UpdatedTimestamp = DateTime.UtcNow`
6. System saves to database
7. System creates Notification:
   - `Type = Activated`
   - `Title = "Trial Converted to Paid Subscription"`
   - `Message = "Your trial has been converted to a paid subscription"`
8. System sends email (if `ContactEmail` exists)
9. System triggers webhooks (EventType: `TrialConverted`)
10. System logs to SubscriptionHistory:
    - `ActionType = TrialConverted`
    - `OldValue = { IsTrial: true, TrialEndDate: "2025-02-14" }`
    - `NewValue = { IsTrial: false, ExpiryDate: "2025-12-31" }`
11. System returns updated company

**API Endpoint**: `POST /api/licensing/{id}/trial/convert`

**Request**:
```json
{
  "expiryDate": "2025-12-31T23:59:59Z"
}
```

**Business Rules**:
- Company must be in trial to convert
- Conversion is seamless (no interruption)
- External systems continue to get "Active" status

**Status After Conversion**:
- `IsTrial = false`
- `IsActive = true`
- `ExpiryDate = 2025-12-31` (paid subscription date)
- `TrialEndDate = null`
- **Status = Active** ✅

---

### Flow 3: Automatic Expiry Process (Background Worker)

#### What Happens Automatically (Daily at Midnight)

**Background Worker**: `SubscriptionExpiryWorker`

**Schedule**: Daily at configurable time (default: 00:00 UTC)

**Step-by-Step Process**:

1. **Worker Starts**:
   - Checks if worker is enabled in configuration
   - Logs start message

2. **Query Expired Companies**:
   ```sql
   SELECT * FROM Companies 
   WHERE ExpiryDate < GETUTCDATE() 
   AND IsExpired = 0 
   AND IsDeleted = 0
   ```

3. **For Each Expired Company**:
   - Set `IsExpired = true`
   - Set `IsActive = false`
   - Update `UpdatedTimestamp = DateTime.UtcNow`
   - Save to database
   - Create Notification:
     - `Type = Expired`
     - `Title = "Subscription Expired"`
     - `Message = "Your subscription has expired"`
   - Send email (if `ContactEmail` exists):
     - Subject: "Subscription Expired"
     - Body: Localized email template
   - Trigger webhooks:
     - Finds active webhooks for company
     - Filters by event: `CompanyExpired`
     - POSTs to webhook URLs
     - Logs delivery attempts
   - Log to SubscriptionHistory:
     - `ActionType = Expired`
     - `OldValue = { IsExpired: false, IsActive: true }`
     - `NewValue = { IsExpired: true, IsActive: false }`
     - `PerformedBy = null` (system action)
     - `Timestamp = DateTime.UtcNow`

4. **Query Trial Expired Companies**:
   ```sql
   SELECT * FROM Companies 
   WHERE IsTrial = 1 
   AND TrialEndDate < GETUTCDATE() 
   AND IsDeleted = 0
   ```

5. **For Each Trial Expired Company**:
   - Set `IsTrial = false`
   - Set `IsActive = false`
   - Set `IsExpired = true`
   - Update `UpdatedTimestamp = DateTime.UtcNow`
   - Save to database
   - Create Notification:
     - `Type = Expired`
     - `Title = "Trial Period Expired"`
     - `Message = "Your trial period has expired"`
   - Send email (if `ContactEmail` exists)
   - Trigger webhooks (EventType: `TrialExpired`)
   - Log to SubscriptionHistory:
     - `ActionType = TrialExpired`
     - `OldValue = { IsTrial: true, IsActive: true }`
     - `NewValue = { IsTrial: false, IsActive: false, IsExpired: true }`

6. **Worker Completes**:
   - Logs completion message with count of expired companies
   - Waits until next scheduled run

**Configuration**: `ExpiryCheckSettings` in `appsettings.json`

**Business Rules**:
- Runs automatically - no admin intervention needed
- Processes all expired companies in batch
- Creates notifications and sends emails
- Triggers webhooks for external systems
- Logs all actions to SubscriptionHistory

**What External Systems See**:
- Next status check returns: `Status = Expired`
- Access is blocked
- Shows localized message: "Subscription has expired" / "انتهت صلاحية الاشتراك"

---

### Flow 4: Expiry Warning Notifications (Background Worker)

#### What Happens Automatically (Daily at 9 AM)

**Background Worker**: `ExpiryNotificationWorker`

**Schedule**: Daily at configurable time (default: 09:00 UTC)

**Step-by-Step Process**:

1. **Worker Starts**:
   - Checks if worker is enabled
   - Logs start message

2. **For Each Warning Day** (7, 3, 1 days before expiry):
   - Query companies expiring in X days:
     ```sql
     SELECT * FROM Companies 
     WHERE ExpiryDate = DATEADD(DAY, X, GETUTCDATE())
     AND IsActive = 1 
     AND IsExpired = 0
     AND IsDeleted = 0
     ```
   - For each company:
     - Check if notification already sent (avoid duplicates)
     - Create Notification:
       - `Type = ExpiryWarning`
       - `Title = "Subscription Expiring Soon"`
       - `Message = "Your subscription expires in {X} days"`
     - Send email (if `ContactEmail` exists):
       - Subject: "Subscription Expiring Soon"
       - Body: Localized email template with days remaining
     - Log notification creation

3. **For Companies Expiring Today**:
   - Query companies where `ExpiryDate = Today`
   - Create Notification:
     - `Type = Expired`
     - `Title = "Subscription Expired Today"`
     - `Message = "Your subscription has expired today"`
   - Send email (if `ContactEmail` exists)

4. **Worker Completes**:
   - Logs completion message
   - Waits until next scheduled run

**Configuration**: `NotificationSettings` in `appsettings.json`

**Business Rules**:
- Sends warnings at 7, 3, and 1 days before expiry
- Avoids duplicate notifications (checks if already sent)
- Only sends to active companies
- Email and in-system notifications both sent

---

### Flow 5: Bulk Operations

#### Flow 5.1: Bulk Activate Multiple Companies

**What Happens**:
1. Admin selects multiple companies (checkboxes)
2. Admin clicks "Bulk Activate" button
3. Admin enters expiry date (applies to all)
4. Admin confirms action
5. System processes each company:
   - Validates company exists and is not deleted
   - Updates: `IsActive = true`, `ExpiryDate = provided_date`
   - Creates notification
   - Triggers webhooks
   - Logs to SubscriptionHistory
6. System aggregates results:
   - Counts successful operations
   - Counts failed operations
   - Collects error messages
7. System returns bulk operation response

**API Endpoint**: `POST /api/licensing/bulk-activate`

**Request**:
```json
{
  "companyIds": ["encrypted_id_1", "encrypted_id_2", "encrypted_id_3"],
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
    "totalRequested": 3,
    "successful": 3,
    "failed": 0,
    "errorMessage": null
  }
}
```

**Response (Partial Failure)**:
```json
{
  "statusCode": 200,
  "message": "Bulk operation completed",
  "data": {
    "totalRequested": 3,
    "successful": 2,
    "failed": 1,
    "errorMessage": "1 company failed: Company not found"
  }
}
```

**Business Rules**:
- Processes all companies even if some fail
- Continues on individual failures
- Returns aggregate result
- Each company gets individual notification and webhook

**What Gets Created for Each Company**:
- Notification (in-system)
- Email notification (if email exists)
- Webhook delivery (if webhooks configured)
- SubscriptionHistory entry

---

#### Flow 5.2: Bulk Suspend/Resume/Extend

**Same Process as Bulk Activate**, but with different actions:
- **Bulk Suspend**: Sets `IsActive = false` for all selected companies
- **Bulk Resume**: Sets `IsActive = true` for all selected companies
- **Bulk Extend**: Updates `ExpiryDate` for all selected companies

**API Endpoints**:
- `POST /api/licensing/bulk-suspend`
- `POST /api/licensing/bulk-resume`
- `POST /api/licensing/bulk-extend`

---

#### Flow 5.3: Bulk Delete Companies

**What Happens**:
1. Admin selects multiple companies
2. Admin clicks "Bulk Delete" button
3. Admin confirms action (with warning)
4. System processes each company:
   - Performs soft delete: `IsDeleted = true`
   - Sets `DeletedTimestamp = DateTime.UtcNow`
   - Logs to SubscriptionHistory:
     - `ActionType = Deleted`
     - `OldValue = { company data }`
     - `NewValue = null`
5. System returns bulk operation response

**API Endpoint**: `POST /api/companies/bulk-delete`

**Business Rules**:
- Soft delete (not permanent)
- Company data is preserved
- Can be restored if needed (future feature)

---

### Flow 6: Subscription Plan Management

#### Flow 6.1: Create Subscription Plan

**What Happens**:
1. Admin navigates to Subscription Plans page
2. Admin clicks "Create Plan" button
3. Admin fills form:
   - Name: "Professional"
   - Description: "Professional plan with all features"
   - Price: 99.99
   - Currency: "USD"
   - Billing Cycle: "Monthly" or "Yearly"
   - Features: ["Feature 1", "Feature 2", ...]
4. Admin clicks "Create"
5. System validates input
6. System creates SubscriptionPlan entity:
   - `IsActive = true` (default)
   - `CreatedTimestamp = DateTime.UtcNow`
7. System saves to database
8. System returns created plan

**API Endpoint**: `POST /api/subscription-plans`

**Request**:
```json
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

**Business Rules**:
- Plan name must be unique
- Price must be positive
- Billing cycle: Monthly, Yearly, Quarterly, or OneTime
- Plan can be assigned to companies

---

#### Flow 6.2: Assign Modules to Plan

**What Happens**:
1. Admin views subscription plan details
2. Admin clicks "Manage Modules" button
3. Admin sees list of available project-module combinations
4. Admin selects modules to include in plan
5. Admin clicks "Save"
6. System updates PlanProjectModule relationships:
   - Deletes existing relationships for this plan
   - Creates new relationships for selected modules
7. System saves to database
8. System returns updated plan with modules

**API Endpoint**: `PUT /api/subscription-plans/{id}/project-modules`

**Request**:
```json
{
  "projectModuleIds": ["encrypted_id_1", "encrypted_id_2", "encrypted_id_3"]
}
```

**Business Rules**:
- Plan can have multiple project-module combinations
- Modules determine what features company can access
- Company's accessible modules = Plan's assigned modules

---

### Flow 7: Project and Module Management

#### Flow 7.1: Create Project

**What Happens**:
1. Admin navigates to Projects page
2. Admin clicks "Create Project" button
3. Admin fills form:
   - Name: "ERP System"
   - Description: "Enterprise Resource Planning"
4. Admin clicks "Create"
5. System creates Project entity
6. System saves to database
7. System returns created project

**API Endpoint**: `POST /api/projects`

**Business Rules**:
- Project name must be unique
- Project can have multiple modules
- Project is linked to modules via ProjectModule junction table

---

#### Flow 7.2: Create Module

**What Happens**:
1. Admin navigates to Modules page
2. Admin clicks "Create Module" button
3. Admin fills form:
   - Name: "Inventory Management"
   - Description: "Inventory tracking and management"
4. Admin clicks "Create"
5. System creates Module entity
6. System saves to database
7. System returns created module

**API Endpoint**: `POST /api/modules`

**Business Rules**:
- Module name must be unique
- Module can belong to multiple projects
- Module is linked to projects via ProjectModule junction table

---

#### Flow 7.3: Link Module to Project

**What Happens**:
1. Admin views project details
2. Admin clicks "Add Module" button
3. Admin selects module from dropdown
4. Admin clicks "Link"
5. System creates ProjectModule relationship:
   - `ProjectId = selected_project_id`
   - `ModuleId = selected_module_id`
6. System saves to database
7. System returns updated project with modules

**API Endpoint**: `POST /api/project-modules`

**Request**:
```json
{
  "projectId": "encrypted_project_id",
  "moduleId": "encrypted_module_id"
}
```

**Business Rules**:
- Many-to-many relationship
- Module can be in multiple projects
- Project can have multiple modules

---

### Flow 8: Company Groups Management

#### Flow 8.1: Create Company Group

**What Happens**:
1. Admin navigates to Company Groups page
2. Admin clicks "Create Group" button
3. Admin fills form:
   - Name: "Enterprise Clients"
   - Description: "Large enterprise customers"
4. Admin clicks "Create"
5. System creates CompanyGroup entity
6. System saves to database
7. System returns created group

**API Endpoint**: `POST /api/company-groups`

**Business Rules**:
- Group name must be unique
- Group can contain multiple companies
- Group enables bulk operations on multiple companies

---

#### Flow 8.2: Add Companies to Group

**What Happens**:
1. Admin views group details
2. Admin clicks "Add Companies" button
3. Admin selects companies (multi-select)
4. Admin clicks "Add"
5. System creates CompanyGroupMember relationships:
   - For each company:
     - `CompanyId = company_id`
     - `CompanyGroupId = group_id`
6. System saves to database
7. System returns updated group with companies

**API Endpoint**: `POST /api/company-groups/{id}/companies`

**Request**:
```json
{
  "companyIds": ["encrypted_id_1", "encrypted_id_2"]
}
```

**Business Rules**:
- Company can be in multiple groups
- Group can have multiple companies
- Enables group-based bulk operations

---

#### Flow 8.3: Bulk Activate Group

**What Happens**:
1. Admin views group details
2. Admin clicks "Bulk Activate Group" button
3. Admin enters expiry date
4. Admin confirms action
5. System gets all companies in group
6. System activates all companies (same as bulk activate)
7. System returns operation result

**API Endpoint**: `POST /api/company-groups/{id}/bulk-activate`

**Business Rules**:
- Activates all companies in group
- Same process as individual bulk activate
- Each company gets individual notification and webhook

---

### Flow 9: Custom Fields Management

#### Flow 9.1: Add Custom Field to Company

**What Happens**:
1. Admin views company details
2. Admin clicks "Add Custom Field" button
3. Admin fills form:
   - Field Name: "Industry"
   - Field Value: "Manufacturing"
   - Field Type: "String" (or Number, Boolean, Date, JSON)
4. Admin clicks "Add"
5. System validates:
   - Field name is required
   - Field value matches field type
6. System creates CompanyCustomField entity:
   - `CompanyId = company_id`
   - `FieldName = "Industry"`
   - `FieldValue = "Manufacturing"`
   - `FieldType = String`
7. System saves to database
8. System returns created custom field

**API Endpoint**: `POST /api/companies/{id}/custom-fields`

**Request**:
```json
{
  "fieldName": "Industry",
  "fieldValue": "Manufacturing",
  "fieldType": "String"
}
```

**Business Rules**:
- Multiple custom fields per company
- Field types: String, Number, Boolean, Date, JSON
- JSON type allows complex data structures
- Custom fields available for reporting and filtering

---

### Flow 10: API Key Management

#### Flow 10.1: Create API Key for Company

**What Happens**:
1. Admin views company details
2. Admin clicks "Create API Key" button
3. Admin fills form:
   - Name: "ERP System API Key"
   - Expires At: (optional) Future date
   - Allowed IPs: (optional) ["192.168.1.0/24", "10.0.0.50"]
   - Rate Limit: (optional) 1000 requests/hour
4. Admin clicks "Create"
5. System generates API key:
   - Generates 32 random bytes
   - Base64 encodes: `sk_live_abc123...`
   - Hashes with SHA256: `SHA256(api_key)`
   - Stores hash (never stores plain key)
6. System creates ApiKey entity:
   - `CompanyId = company_id`
   - `KeyHash = hashed_key`
   - `KeyPrefix = "sk_live_"` (first 8 chars for display)
   - `Name = "ERP System API Key"`
   - `IsActive = true`
   - `ExpiresAt = provided_date` (if provided)
   - `AllowedIps = provided_ips` (if provided)
   - `RateLimitPerHour = provided_limit` (if provided)
7. System saves to database
8. System returns API key (shown only once):
   ```json
   {
     "key": "sk_live_abc123...",
     "keyPrefix": "sk_live_",
     "message": "API key created successfully. Store this key securely - it will not be shown again."
   }
   ```

**API Endpoint**: `POST /api/api-keys`

**Request**:
```json
{
  "companyId": "encrypted_company_id",
  "name": "ERP System API Key",
  "expiresAt": "2025-12-31T23:59:59Z",
  "allowedIps": ["192.168.1.0/24"],
  "rateLimitPerHour": 1000
}
```

**Business Rules**:
- Key is shown only once - must be copied immediately
- Key is hashed before storage (SHA256)
- Only key prefix is stored for display
- IP whitelist supports CIDR notation
- Rate limit is per API key

**Security**:
- Key never stored in plain text
- Key hash is unique (prevents duplicates)
- IP whitelist enforced by middleware
- Rate limit enforced by middleware

---

#### Flow 10.2: External System Uses API Key

**What Happens** (External system perspective):
1. External system makes API request:
   ```http
   GET /api/licensing/{company_id}/status
   Authorization: Bearer sk_live_abc123...
   ```
2. System receives request
3. System extracts API key from Authorization header
4. System hashes key: `SHA256(api_key)`
5. System finds ApiKey by hash:
   ```sql
   SELECT * FROM ApiKeys 
   WHERE KeyHash = @hashed_key 
   AND IsActive = 1 
   AND IsDeleted = 0
   ```
6. System validates:
   - Key exists and is active
   - Key not expired (`ExpiresAt > Now`)
   - Request IP matches whitelist (if set)
   - Rate limit not exceeded
7. System extracts `CompanyId` from API key
8. System sets company context for request
9. System processes request normally
10. System updates `LastUsedAt = DateTime.UtcNow`
11. System returns response

**Business Rules**:
- API key authentication happens before JWT authentication
- Company context is set from API key
- Rate limiting is per API key
- IP whitelist is enforced

---

### Flow 11: Webhook Management

#### Flow 11.1: Register Webhook

**What Happens**:
1. Admin views company details
2. Admin clicks "Add Webhook" button
3. Admin fills form:
   - URL: "https://external-system.com/webhook"
   - Secret: "webhook_secret_key"
   - Events: Select events (Activated, Suspended, Expired, etc.)
4. Admin clicks "Create"
5. System validates:
   - URL is valid format
   - Secret is provided
   - At least one event selected
6. System creates Webhook entity:
   - `CompanyId = company_id`
   - `Url = "https://external-system.com/webhook"`
   - `Secret = "webhook_secret_key"`
   - `Events = ["CompanyActivated", "CompanySuspended", ...]`
   - `IsActive = true`
7. System saves to database
8. System returns created webhook

**API Endpoint**: `POST /api/webhooks`

**Request**:
```json
{
  "companyId": "encrypted_company_id",
  "url": "https://external-system.com/webhook",
  "secret": "webhook_secret_key",
  "events": ["CompanyActivated", "CompanySuspended", "CompanyExpired"],
  "isActive": true
}
```

**Business Rules**:
- Webhook URL must be accessible
- Secret is used for HMAC signature
- Events determine which events trigger webhook
- Webhook can be active or inactive

---

#### Flow 11.2: Webhook Delivery (Automatic)

**What Happens When Event Occurs** (e.g., Company Activated):
1. System calls `IWebhookService.TriggerWebhookAsync()`
2. System finds active webhooks for company:
   ```sql
   SELECT * FROM Webhooks 
   WHERE CompanyId = @company_id 
   AND IsActive = 1
   AND JSON_CONTAINS(Events, @event_type)
   ```
3. For each webhook:
   - Create WebhookDelivery record:
     - `WebhookId = webhook_id`
     - `EventType = "CompanyActivated"`
     - `Payload = { event data JSON }`
     - `AttemptedAt = DateTime.UtcNow`
     - `Succeeded = false` (initially)
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
   - Compute HMAC signature:
     ```
     signature = HMAC_SHA256(payload_json, webhook_secret)
     ```
   - POST to webhook URL:
     ```http
     POST https://external-system.com/webhook
     X-Webhook-Signature: {signature}
     X-Webhook-Event: CompanyActivated
     Content-Type: application/json
     
     { payload JSON }
     ```
   - Update WebhookDelivery:
     - `StatusCode = http_response_code`
     - `ResponseBody = response_body`
     - `Succeeded = true` if 200-299, `false` otherwise
4. If delivery failed:
   - Retry after 1 minute
   - Retry after 5 minutes
   - Retry after 30 minutes
   - Maximum 3 retries (configurable)

**Business Rules**:
- Webhooks are triggered automatically on events
- HMAC signature ensures authenticity
- Retry logic handles temporary failures
- All deliveries are logged

---

### Flow 12: Reports and Analytics

#### Flow 12.1: Generate Subscription Expiry Report

**What Happens**:
1. Admin navigates to Reports page
2. Admin selects "Subscription Expiry Report"
3. Admin enters parameters:
   - Days: 30 (companies expiring in next 30 days)
4. Admin clicks "Generate Report"
5. System queries companies:
   ```sql
   SELECT * FROM Companies 
   WHERE ExpiryDate BETWEEN GETUTCDATE() AND DATEADD(DAY, 30, GETUTCDATE())
   AND IsDeleted = 0
   ORDER BY ExpiryDate ASC
   ```
6. System formats report data:
   - Company Name
   - Expiry Date
   - Days Until Expiry
   - Status
   - Contact Email
7. System returns report data

**API Endpoint**: `POST /api/reports/SubscriptionExpiry/generate`

**Request**:
```json
{
  "days": 30
}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "Report generated successfully",
  "data": {
    "reportType": "SubscriptionExpiry",
    "generatedAt": "2025-01-15T10:00:00Z",
    "parameters": { "days": 30 },
    "results": [
      {
        "companyId": "encrypted_id",
        "companyName": "Acme Corp",
        "expiryDate": "2025-01-20T00:00:00Z",
        "daysUntilExpiry": 5,
        "status": "Active"
      },
      // ... more companies
    ]
  }
}
```

**Business Rules**:
- Report shows companies expiring in specified days
- Results sorted by expiry date (soonest first)
- Includes all relevant company information

---

#### Flow 12.2: Download Report

**What Happens**:
1. Admin views generated report
2. Admin clicks "Download" button
3. Admin selects format: CSV or Excel
4. System formats data according to format
5. System generates file:
   - CSV: Comma-separated values
   - Excel: XLSX format using DocumentFormat.OpenXml
6. System returns file download

**API Endpoint**: `GET /api/reports/SubscriptionExpiry/download?format=excel`

**Business Rules**:
- Supports CSV and Excel formats
- File includes all report columns
- File name includes report type and date

---

### Flow 13: Notifications Management

#### Flow 13.1: View Notifications

**What Happens**:
1. Admin navigates to Notifications page
2. System fetches notifications: `GET /api/notifications?page=1&pageSize=10`
3. System displays notifications:
   - Unread notifications highlighted
   - Grouped by company (optional)
   - Sorted by date (newest first)
4. Admin can filter:
   - By company
   - Unread only
   - By type (ExpiryWarning, Expired, etc.)

**API Endpoint**: `GET /api/notifications?page=1&pageSize=10&unreadOnly=false`

**Response**:
```json
{
  "statusCode": 200,
  "message": "",
  "data": {
    "items": [
      {
        "id": "encrypted_id",
        "companyId": "encrypted_company_id",
        "companyName": "Acme Corporation",
        "type": "ExpiryWarning",
        "title": "Subscription Expiring Soon",
        "message": "Subscription expires in 3 days",
        "isRead": false,
        "createdAt": "2025-01-15T10:30:00Z"
      },
      // ... more notifications
    ],
    "totalCount": 45,
    "page": 1,
    "pageSize": 10,
    "totalPages": 5
  }
}
```

**Business Rules**:
- Notifications are paginated
- Unread notifications are highlighted
- Notifications can be filtered and sorted

---

#### Flow 13.2: Mark Notification as Read

**What Happens**:
1. Admin views notification
2. Admin clicks notification (or "Mark as Read" button)
3. System updates notification:
   - `IsRead = true`
   - `ReadAt = DateTime.UtcNow`
4. System saves to database
5. System returns updated notification

**API Endpoint**: `PUT /api/notifications/{id}/read`

**Business Rules**:
- Notification can be marked as read
- Read status is permanent
- Unread count decreases

---

### Flow 14: Analytics and Monitoring

#### Flow 14.1: View Company Usage Analytics

**What Happens**:
1. Admin views company details
2. Admin clicks "Usage Analytics" tab
3. System fetches analytics: `GET /api/analytics/company/{id}/usage?fromDate={date}&toDate={date}`
4. System displays:
   - Total requests
   - Requests by endpoint
   - Average response time
   - Requests by date (time series)
5. System shows charts:
   - Bar chart: Requests by endpoint
   - Line chart: Requests over time

**API Endpoint**: `GET /api/analytics/company/{id}/usage?fromDate=2025-01-01&toDate=2025-01-31`

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
      "2025-01-02": 520,
      // ... more dates
    }
  }
}
```

**Business Rules**:
- Analytics are tracked automatically by middleware
- Data is aggregated by date
- Shows usage patterns and trends

---

### Flow 15: Search Functionality

#### Flow 15.1: Global Search

**What Happens**:
1. Admin types in search box (top header)
2. System searches across entities:
   - Companies
   - Admins
   - Subscription Plans
   - Projects
   - Modules
3. System returns results grouped by entity type
4. Admin clicks result to navigate

**API Endpoint**: `POST /api/search`

**Request**:
```json
{
  "query": "Acme",
  "page": 1,
  "pageSize": 10,
  "entityTypes": ["Company", "Admin"],
  "includeInactive": false
}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "",
  "data": {
    "results": [
      {
        "entityType": "Company",
        "entityId": "encrypted_id",
        "title": "Acme Corporation",
        "description": "Active subscription",
        "highlight": "Acme Corporation"
      },
      // ... more results
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 10
  }
}
```

**Business Rules**:
- Searches across multiple entity types
- Results are paginated
- Can filter by entity type
- Highlights matching text

---

## 🔄 Status Calculation Logic (Critical Business Rule)

### How Status is Calculated

SYNFLOX uses a **priority-based status calculation**:

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

### Status Scenarios

| IsActive | ExpiryDate | IsExpired | IsTrial | TrialEndDate | **Status** |
|----------|------------|-----------|---------|--------------|------------|
| true | 2025-12-31 (future) | false | false | null | **Active** ✅ |
| true | 2024-01-01 (past) | true | false | null | **Expired** 🔴 |
| false | 2025-12-31 (future) | false | false | null | **Suspended** ⏸️ |
| true | 2025-12-31 (future) | false | true | 2025-02-14 (future) | **Active** ✅ (Trial) |
| true | 2025-12-31 (future) | false | true | 2024-01-01 (past) | **Expired** 🔴 |

---

## 📊 Data Flow Diagrams

### Company Creation Flow

```
Admin → POST /api/companies
  ↓
Controller validates request
  ↓
Service creates Company entity
  ↓
Repository saves to database
  ↓
Service logs to SubscriptionHistory
  ↓
Service maps to DTO (encrypts ID)
  ↓
Controller returns response
  ↓
Admin sees company in list
```

### Activation Flow

```
Admin → PUT /api/licensing/{id}/activate
  ↓
Controller decrypts ID
  ↓
Service loads company
  ↓
Service updates: IsActive = true, ExpiryDate = ...
  ↓
Repository saves to database
  ↓
Service creates Notification
  ↓
Service sends email (async)
  ↓
Service triggers webhooks (async)
  ↓
Service logs to SubscriptionHistory
  ↓
Service maps to DTO
  ↓
Controller returns response
  ↓
External system checks status → Active ✅
```

### Automatic Expiry Flow

```
Background Worker (Daily at 00:00)
  ↓
Query expired companies
  ↓
For each expired company:
  ↓
  Update: IsExpired = true, IsActive = false
  ↓
  Create Notification
  ↓
  Send email (async)
  ↓
  Trigger webhooks (async)
  ↓
  Log to SubscriptionHistory
  ↓
Worker completes
  ↓
External system checks status → Expired 🔴
```

---

## 🎯 Key Business Rules Summary

### Company Lifecycle Rules

1. **Creation**: Company is created with `IsActive = false` (must be activated)
2. **Activation**: Sets `IsActive = true` and `ExpiryDate` (required)
3. **Suspension**: Sets `IsActive = false` (doesn't change expiry date)
4. **Resume**: Sets `IsActive = true` (if expiry date is valid)
5. **Extension**: Updates `ExpiryDate` to future date
6. **Expiry**: Background worker sets `IsExpired = true` and `IsActive = false`

### Trial Rules

1. **Start Trial**: Sets `IsTrial = true`, `TrialEndDate = Now + days`, `IsActive = true`
2. **Convert Trial**: Sets `IsTrial = false`, sets `ExpiryDate`, `IsActive = true`
3. **Trial Expiry**: Background worker sets `IsTrial = false`, `IsActive = false`, `IsExpired = true`

### Notification Rules

1. **Expiry Warnings**: Sent at 7, 3, and 1 days before expiry
2. **Expiry Notification**: Sent when subscription expires
3. **Status Changes**: Notification sent for all status changes (Activated, Suspended, Resumed, Extended)

### Webhook Rules

1. **Event Triggering**: Webhooks triggered automatically on events
2. **HMAC Signing**: All webhooks signed with HMAC SHA256
3. **Retry Logic**: 3 retries with exponential backoff (1min, 5min, 30min)
4. **Delivery Logging**: All delivery attempts logged

### Audit Rules

1. **All Actions Logged**: Every subscription change logged to SubscriptionHistory
2. **JSON Snapshots**: Old and new values stored as JSON
3. **Admin Tracking**: Admin who performed action is recorded
4. **System Actions**: Background worker actions logged with `PerformedBy = null`

---

## 🔐 Security and Authorization

### Authorization Policies

- **SuperAdminOnly**: Only SuperAdmin can access
- **AdminOrSuperAdmin**: Both Admin and SuperAdmin can access
- **AllowAnonymous**: Public endpoint (status checks)

### API Key Security

- Keys are hashed (SHA256) before storage
- Only key prefix shown after creation
- IP whitelisting supported (CIDR notation)
- Per-key rate limiting
- HMAC request signing supported

### Password Security

- NIST 800-63B compliant policies
- Password history tracking
- Password expiration (90 days default)
- Account lockout after failed attempts

---

## 📈 Monitoring and Analytics

### Real-Time Metrics

- Request count
- Response time
- Error rate
- Active companies
- Expired companies
- Suspended companies

### Usage Analytics

- API usage by company
- API usage by endpoint
- Usage trends over time
- Response time analysis

### Error Tracking

- Structured error logging
- Error ID tracking
- Stack trace capture
- Request context capture

---

## 🎯 Complete Admin Workflow Summary

### Daily Admin Tasks

1. **Morning** (9:00 AM):
   - Check dashboard for expired companies
   - Review notifications (expiry warnings)
   - Check webhook delivery status

2. **Throughout Day**:
   - Create new companies
   - Activate subscriptions
   - Handle extension requests
   - Monitor API usage

3. **Evening**:
   - Review daily reports
   - Check analytics
   - Review error logs

### Weekly Admin Tasks

1. Review subscription expiry report (next 30 days)
2. Check trial conversion rates
3. Review API usage trends
4. Audit subscription history
5. Review webhook delivery success rates

### Monthly Admin Tasks

1. Generate monthly reports
2. Review revenue by plan
3. Analyze module usage
4. Review system metrics
5. Audit API keys

---

## 📋 Complete Feature Checklist for Admin Panel

### Company Management
- [x] Create company
- [x] View company list (with pagination, search, filters)
- [x] View company details
- [x] Update company
- [x] Delete company (soft delete)
- [x] Bulk operations (activate, suspend, resume, extend, delete)
- [x] Export companies (CSV, Excel)
- [x] Import companies (CSV, Excel)

### Subscription Management
- [x] Activate subscription
- [x] Suspend subscription
- [x] Resume subscription
- [x] Extend subscription
- [x] Start trial period
- [x] Convert trial to paid
- [x] Generate license key
- [x] Regenerate license key
- [x] View subscription history

### Plan Management
- [x] Create subscription plan
- [x] View plan list
- [x] Update plan
- [x] Delete plan
- [x] Assign modules to plan
- [x] View plan modules

### Project & Module Management
- [x] Create project
- [x] Create module
- [x] Link module to project
- [x] View projects and modules
- [x] Update projects and modules
- [x] Delete projects and modules

### Group Management
- [x] Create company group
- [x] Add companies to group
- [x] Remove companies from group
- [x] Bulk activate group
- [x] Bulk suspend group
- [x] Bulk extend group

### Custom Fields
- [x] Add custom field to company
- [x] View company custom fields
- [x] Update custom field
- [x] Delete custom field

### API Keys
- [x] Create API key
- [x] View API keys
- [x] Update API key
- [x] Regenerate API key
- [x] Revoke API key

### Webhooks
- [x] Register webhook
- [x] View webhooks
- [x] Delete webhook
- [x] View webhook delivery history
- [x] Retry failed webhooks

### Notifications
- [x] View notifications
- [x] Mark notification as read
- [x] Filter notifications
- [x] View unread count

### Reports
- [x] Generate subscription expiry report
- [x] Generate status summary report
- [x] Generate usage analytics report
- [x] Generate trial conversion report
- [x] Generate module usage report
- [x] Download reports (CSV, Excel)

### Analytics
- [x] View company usage analytics
- [x] View API usage by endpoint
- [x] View API usage by company
- [x] View usage trends

### Dashboard
- [x] View system statistics
- [x] View status distribution chart
- [x] View expiry timeline chart
- [x] View API usage chart
- [x] View recent activity

### Search
- [x] Global search across entities
- [x] Search suggestions
- [x] Filter by entity type

### Admin Management
- [x] Create admin
- [x] View admin list
- [x] Update admin
- [x] Delete admin
- [x] Activate/deactivate admin
- [x] Reset password
- [x] View own profile
- [x] Update own profile
- [x] Change own password

---

## 🎯 Best Practices for Admin Panel

### For Administrators

1. **Regular Reviews**: Check expired/suspended companies weekly
2. **Bulk Operations**: Use bulk operations for efficiency
3. **Trial Management**: Monitor trial conversions
4. **Webhook Testing**: Test webhooks before production use
5. **API Key Rotation**: Rotate API keys periodically
6. **Report Generation**: Generate reports regularly for insights
7. **Notification Monitoring**: Check notifications daily
8. **Analytics Review**: Review usage analytics weekly

### For Frontend Developers

1. **Real-Time Updates**: Auto-refresh dashboard every 5 minutes
2. **Loading States**: Show skeletons during data fetch
3. **Error Handling**: Display user-friendly error messages
4. **Confirmation Dialogs**: Confirm destructive actions
5. **Success Notifications**: Show success messages after operations
6. **Pagination**: Implement pagination for large lists
7. **Search & Filters**: Enable search and filtering everywhere
8. **Responsive Design**: Ensure mobile-friendly interface

---

**This guide covers all business flows in the SYNFLOX Admin Panel. Every operation, every step, every business rule is documented here.**

For technical API details, refer to `README.md` and `FRONTEND_IMPLEMENTATION_GUIDE.md`.

