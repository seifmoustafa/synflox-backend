---
description: How to implement the 7 major SaaS features for SYNFLOX
---

# SaaS Features Implementation Workflow

This document outlines the step-by-step implementation of 7 major SaaS features for the SYNFLOX Central Licensing System.

## Prerequisites

- .NET 8 SDK installed
- Node.js 18+ installed
- SQL Server running
- Project cloned and restored

## Overview

| # | Feature | Estimated Time |
|---|---------|---------------|
| 1 | SignalR Real-Time Updates | 4 hours |
| 2 | Webhook System | 8 hours |
| 3 | Mobile Push Notifications | 4 hours |
| 4 | SignalR Redis Backplane | 2 hours |
| 5 | Advanced Analytics | 8 hours |
| 6 | Payments & Billing (Mock Mode) | 22 hours |
| 7 | Audit Trail / Sent Message History | 6 hours |

---

## Feature 1: SignalR Real-Time Updates

### Step 1.1: Create NotificationHub
Create file: `core/SYNFLOX.Core.Infrastructure/Hubs/NotificationHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
    
    public async Task JoinCompanyGroup(string companyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"company-{companyId}");
    }
}
```

### Step 1.2: Register SignalR in Admin API
Edit: `admin-api/src/SYNFLOX.Admin.WebAPI/Program.cs`

```csharp
// Add after builder.Services.AddControllers()
builder.Services.AddSignalR();

// Add before app.Run()
app.MapHub<NotificationHub>("/notificationHub");
```

### Step 1.3: Register SignalR in Client API
Edit: `client-api/src/SYNFLOX.Client.WebAPI/Program.cs`
- Same changes as Admin API

### Step 1.4: Inject Hub into NotificationAppService
- Add `IHubContext<NotificationHub>` to constructor
- After saving notification, broadcast to user group

// turbo
### Step 1.5: Build and verify
```powershell
cd synflox-backend
dotnet build
```

---

## Feature 2: Webhook System

### Step 2.1: Create Webhook Entities
Create: `core/SYNFLOX.Core.Domain/Entities/Webhooks/WebhookEndpoint.cs`
Create: `core/SYNFLOX.Core.Domain/Entities/Webhooks/WebhookEvent.cs`

### Step 2.2: Create WebhookEventStatus Enum
Create: `core/SYNFLOX.Core.Domain/Enums/WebhookEventStatus.cs`

### Step 2.3: Create IWebhookService Interface
Create: `core/SYNFLOX.Core.Application/Services Interfaces/IWebhookService.cs`

### Step 2.4: Implement WebhookService
Create: `core/SYNFLOX.Core.Infrastructure/Services/WebhookService.cs`
- Include HMAC signature generation
- Include retry logic (max 3 attempts)

### Step 2.5: Create WebhookController
Create: `admin-api/src/SYNFLOX.Admin.WebAPI/Controllers/WebhookController.cs`

### Step 2.6: Add Webhook Triggers
Modify services to trigger webhooks:
- SubscriptionService → subscription events
- OfflineLicenseService → device events
- PaymentService → payment events

### Step 2.7: Create Admin UI
Create: `apps/admin/views/webhooks-view.tsx`
Create: `apps/admin/views/webhook-details-view.tsx`

---

## Feature 3: Mobile Push Notifications

### Step 3.1: Create PushSubscription Entity
Create: `core/SYNFLOX.Core.Domain/Entities/Notifications/PushSubscription.cs`

### Step 3.2: Create PushPlatform Enum
Create: `core/SYNFLOX.Core.Domain/Enums/PushPlatform.cs`

### Step 3.3: Create IPushNotificationService
Create: `core/SYNFLOX.Core.Application/Services Interfaces/IPushNotificationService.cs`

### Step 3.4: Implement PushNotificationService
Create: `core/SYNFLOX.Core.Infrastructure/Services/PushNotificationService.cs`
- Check config before sending (disabled by default)
- FCM integration ready but toggled off

### Step 3.5: Add Configuration
Edit: `appsettings.json` (both APIs)
```json
{
  "PushNotifications": {
    "Enabled": false,
    "FirebaseProjectId": "",
    "FirebasePrivateKey": ""
  }
}
```

### Step 3.6: Create Push Endpoints
Add to existing notification controller or create new push controller

---

## Feature 4: SignalR Redis Backplane

### Step 4.1: Add NuGet Package
```powershell
cd core/SYNFLOX.Core.Infrastructure
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

### Step 4.2: Add Configuration
Edit: `appsettings.json`
```json
{
  "SignalR": {
    "UseRedis": false,
    "RedisConnection": "localhost:6379"
  }
}
```

### Step 4.3: Conditional Redis Setup
Edit: `Program.cs` (both APIs)
```csharp
var signalRBuilder = builder.Services.AddSignalR();
if (builder.Configuration.GetValue<bool>("SignalR:UseRedis"))
{
    signalRBuilder.AddStackExchangeRedis(builder.Configuration["SignalR:RedisConnection"]!);
}
```

---

## Feature 5: Advanced Analytics

### Step 5.1: Create Analytics DTOs
Create: `core/SYNFLOX.Core.Application/DTOs/Analytics/AnalyticsDtos.cs`
- MrrSummary
- TimeSeriesDataPoint
- SubscriptionTrendsDto

### Step 5.2: Extend IDashboardService
Add methods:
- GetMrrSummaryAsync()
- GetTrendsAsync(int days)
- GetTopCompaniesByRevenueAsync()
- ExportAnalyticsToPdfAsync()
- ExportAnalyticsToExcelAsync()

### Step 5.3: Implement Analytics Methods
Edit: `DashboardService.cs`

### Step 5.4: Add Analytics Endpoints
Edit: `DashboardController.cs`

### Step 5.5: Create Analytics UI
Create: `apps/admin/views/analytics-view.tsx`
- Charts for trends
- MRR widget
- Export buttons

---

## Feature 6: Payments & Billing (Mock Mode)

### Step 6.1: Create Billing Entities
Create: `core/SYNFLOX.Core.Domain/Entities/Billing/Invoice.cs`
Create: `core/SYNFLOX.Core.Domain/Entities/Billing/PaymentTransaction.cs`

### Step 6.2: Create Billing Enums
Create: `core/SYNFLOX.Core.Domain/Enums/InvoiceStatus.cs`
Create: `core/SYNFLOX.Core.Domain/Enums/TransactionType.cs`
Create: `core/SYNFLOX.Core.Domain/Enums/TransactionStatus.cs`

### Step 6.3: Create IPaymentService
Create: `core/SYNFLOX.Core.Application/Services Interfaces/IPaymentService.cs`

### Step 6.4: Create IPaymentGateway Interface
Create: `core/SYNFLOX.Core.Application/Services Interfaces/IPaymentGateway.cs`

### Step 6.5: Implement MockPaymentGateway
Create: `core/SYNFLOX.Core.Infrastructure/Services/Payment/MockPaymentGateway.cs`
- Always returns success

### Step 6.6: Implement PaymentService
Create: `core/SYNFLOX.Core.Infrastructure/Services/Payment/PaymentService.cs`
- CreateInvoice
- MarkAsPaid (admin action)
- GenerateRenewalInvoices

### Step 6.7: Create BillingController (Admin)
Create: `admin-api/src/SYNFLOX.Admin.WebAPI/Controllers/BillingController.cs`

### Step 6.8: Create ClientBillingController
Create: `client-api/src/SYNFLOX.Client.WebAPI/Controllers/ClientBillingController.cs`

### Step 6.9: Add Invoice PDF Generation
Create: `core/SYNFLOX.Core.Infrastructure/Services/Payment/InvoicePdfService.cs`

### Step 6.10: Create Admin Billing UI
Create: `apps/admin/views/billing/invoices-view.tsx`
Create: `apps/admin/views/billing/invoice-details-view.tsx`
- "Mark as Paid" button for admin

### Step 6.11: Create Client Billing UI
Create: `apps/client/views/billing-view.tsx`
- View invoices, download PDFs

### Step 6.12: Add Billing Configuration
Edit: `appsettings.json`
```json
{
  "Billing": {
    "Mode": "Manual",
    "AutoGenerateInvoices": true,
    "InvoicePrefix": "INV",
    "DueDays": 30,
    "TaxRate": 0.14
  }
}
```

---

## Feature 7: Audit Trail / Sent Message History

### Step 7.1: Create SentNotification Entity
Create: `core/SYNFLOX.Core.Domain/Entities/Notifications/SentNotification.cs`

### Step 7.2: Create DeliveryStatus Enum
Create: `core/SYNFLOX.Core.Domain/Enums/DeliveryStatus.cs`

### Step 7.3: Extend INotificationAppService
Add methods:
- GetSentHistoryAsync()
- GetSentStatsAsync()
- RetryFailedDeliveriesAsync()

### Step 7.4: Implement Sent History
Edit: `NotificationAppService.cs`
- Log every send to SentNotification table
- Track status per channel

### Step 7.5: Add Sent History Endpoints
Edit: `NotificationsController.cs`
```
GET  /api/notifications/sent
GET  /api/notifications/sent/{id}
POST /api/notifications/sent/{id}/retry
```

### Step 7.6: Create Sent Messages UI
Create: `apps/admin/views/sent-messages-view.tsx`
- Filter by date, company, status
- Retry failed deliveries

---

## Database Migrations

After creating all entities, generate EF Core migration:

// turbo
```powershell
cd core/SYNFLOX.Core.Infrastructure
dotnet ef migrations add AddSaaSFeatures --startup-project ../../admin-api/src/SYNFLOX.Admin.WebAPI
```

// turbo
```powershell
dotnet ef database update --startup-project ../../admin-api/src/SYNFLOX.Admin.WebAPI
```

---

## Verification

// turbo
### Build Backend
```powershell
cd synflox-backend
dotnet build
```

// turbo
### Build Frontend
```powershell
cd synflox-frontend
npm run build
```

### Manual Testing Checklist
- [ ] SignalR: Notification appears in real-time
- [ ] Webhooks: Test webhook delivers to endpoint
- [ ] Push: Toggle respects config (disabled = no calls)
- [ ] Analytics: MRR displays correct values
- [ ] Billing: Create invoice, mark as paid
- [ ] Audit: Sent messages appear in history
