# SYNFLOX Microservices - Local Development Guide

## Architecture Overview

```
┌─────────────────────────┐                    ┌─────────────────────────┐
│      ADMIN API          │                    │      CLIENT API         │
│    (port 5035)          │                    │     (port 5036)         │
│                         │                    │                         │
│  - Creates companies    │                    │  - License validation   │
│  - Manages subscriptions│    AUTO SYNC      │  - Device registration  │
│  - Generates licenses   │  ─────────────►   │  - Entitlement checks   │
│  - Admin dashboard      │   (every 5 sec)   │  - Online token ops     │
│                         │                    │                         │
└───────────┬─────────────┘                    └───────────┬─────────────┘
            │                                              │
            ▼                                              ▼
┌─────────────────────────┐                    ┌─────────────────────────┐
│   SYNFLOX (Master DB)   │                    │ SYNFLOX_Client (Replica)│
│                         │                    │                         │
│  - Admin writes here    │  DataSyncJob       │  - Client reads here    │
│  - Source of truth      │  ─────────────►   │  - Auto-updated copy    │
│                         │                    │  - Cache invalidated    │
└─────────────────────────┘                    └─────────────────────────┘
```

## How It Works

1. **Admin API** writes all data to `SYNFLOX` database (Master)
2. **DataSyncBackgroundJob** runs every 5 seconds in Admin API
3. Job syncs modified records from `SYNFLOX` → `SYNFLOX_Client`
4. **CacheInvalidationJob** in Client API detects new sync
5. Client cache is cleared, fresh data served on next request
6. **Client API** reads from `SYNFLOX_Client` (Replica)

## Running Both APIs Locally

### Prerequisites
- Both databases created: `SYNFLOX` and `SYNFLOX_Client`
- Migrations applied to both (via Package Manager Console)

### Terminal 1 - Admin API (writes to Master)
```powershell
cd E:\Proj\SYNFLOX-Project\synflox-backend\admin-api\src\SYNFLOX.Admin.WebAPI
dotnet run
```
**URL:** https://localhost:5035/swagger

### Terminal 2 - Client API (reads from Replica)
```powershell
cd E:\Proj\SYNFLOX-Project\synflox-backend\client-api\src\SYNFLOX.Client.WebAPI
dotnet run
```
**URL:** https://localhost:5036/swagger

### Terminal 3 - Admin Frontend (optional)
```powershell
cd E:\Proj\SYNFLOX-Project\synflox-frontend\apps\admin
npm run dev
```
**URL:** http://localhost:3000

### Terminal 4 - Client Frontend (optional)
```powershell
cd E:\Proj\SYNFLOX-Project\synflox-frontend\apps\client
npm run dev
```
**URL:** http://localhost:3001

## Testing the Sync

1. **Start both APIs** (Terminal 1 + 2)

2. **Check Client sync status:**
   ```
   GET https://localhost:5036/api/SyncStatus
   ```
   Initially shows "NoSync" until Admin makes changes.

3. **Create something in Admin:**
   - Go to https://localhost:5035/swagger
   - Create a company, subscription, etc.

4. **Wait 5 seconds** (sync interval)

5. **Check sync status again:**
   ```
   GET https://localhost:5036/api/SyncStatus
   ```
   Shows synced tables and timestamps.

6. **Client API now has the data!**
   - Query the Client API endpoints
   - Data should be available

## Configuration

### Admin API (appsettings.json)
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "...Database=SYNFLOX...",      // Master DB (writes)
    "ClientDbConnection": "...Database=SYNFLOX_Client..." // Replica DB (sync target)
  },
  "DataSyncSettings": {
    "Enabled": true,
    "SyncIntervalSeconds": 5,
    "BatchSize": 1000
  }
}
```

### Client API (appsettings.json)
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "...Database=SYNFLOX_Client..." // Replica DB (reads)
  }
}
```

## Sync Internals

### Tables Synced (in dependency order)
1. AdminTypes, Projects, Modules
2. Admins, ProjectModules, SubscriptionPlans
3. Companies, PlanEntitlements, PlanPrices
4. CompanyAdmins, Subscriptions, ClientAccessTokens
5. SubscriptionEntitlements, Licenses, LicenseActivations
6. Supporting tables (RefreshTokens, SecurityAuditLogs, etc.)

### Sync Tracking
- `SyncMetadata` table created in Replica DB
- Tracks: TableName, LastSyncTime, SyncedRecords
- Uses `ModifiedTimestamp` from entities to detect changes

### Cache Invalidation
- CacheInvalidationJob checks SyncMetadata every 2 seconds
- When new sync detected, clears memory cache
- Next API request fetches fresh data from DB

## Free Local Development

This setup is **100% FREE** for local development:
- Uses SQL Server LocalDB or Express
- No paid replication licenses needed
- Background jobs handle sync automatically
- Works exactly like production would

## Production Deployment

For production, you have options:
1. **Keep this setup** - Background job sync works fine
2. **Use SQL Server Replication** - Built-in, requires Standard/Enterprise
3. **Use Change Data Capture** - More advanced, real-time
4. **Single DB** - Both APIs point to same DB (simplest)
