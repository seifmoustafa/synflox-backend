---
description: Quick reference for running common backend and frontend commands
---

# SYNFLOX Development Commands

## Backend Commands

// turbo-all
### Build all projects
```powershell
cd synflox-backend
dotnet build
```

### Run Admin API
```powershell
cd synflox-backend/admin-api/src/SYNFLOX.Admin.WebAPI
dotnet run
```

### Run Client API
```powershell
cd synflox-backend/client-api/src/SYNFLOX.Client.WebAPI
dotnet run
```

### Add EF Migration
```powershell
cd synflox-backend/core/SYNFLOX.Core.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../../admin-api/src/SYNFLOX.Admin.WebAPI
```

### Update Database
```powershell
cd synflox-backend/core/SYNFLOX.Core.Infrastructure
dotnet ef database update --startup-project ../../admin-api/src/SYNFLOX.Admin.WebAPI
```

## Frontend Commands

// turbo-all
### Install dependencies
```powershell
cd synflox-frontend
npm install
```

### Run Admin Portal (dev)
```powershell
cd synflox-frontend
npm run dev --workspace=apps/admin
```

### Run Client Portal (dev)
```powershell
cd synflox-frontend
npm run dev --workspace=apps/client
```

### TypeScript check
```powershell
cd synflox-frontend
npx tsc --noEmit --project apps/admin/tsconfig.json
npx tsc --noEmit --project apps/client/tsconfig.json
```

### Build all
```powershell
cd synflox-frontend
npm run build
```
