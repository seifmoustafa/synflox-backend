@echo off
REM ============================================================
REM SYNFLOX Database Migration Script
REM Creates and updates both Master (SYNFLOX) and Replica (SYNFLOX_Client) databases
REM ============================================================

echo.
echo ============================================================
echo    SYNFLOX Database Migration Script
echo ============================================================
echo.

REM Navigate to Infrastructure project
cd /d "e:\Proj\SYNFLOX-Project\synflox-backend\core\SYNFLOX.Core.Infrastructure"

echo [1/2] Updating Master Database (SYNFLOX)...
echo.
dotnet ef database update --startup-project "..\..\admin-api\src\SYNFLOX.Admin.WebAPI" --context ApplicationDBContext --connection "Server=.\Seif;Database=SYNFLOX;Trusted_Connection=True;TrustServerCertificate=True;"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to update Master database!
    pause
    exit /b 1
)
echo.
echo [OK] Master database updated successfully!
echo.

echo [2/2] Updating Replica Database (SYNFLOX_Client)...
echo.
dotnet ef database update --startup-project "..\..\admin-api\src\SYNFLOX.Admin.WebAPI" --context ApplicationDBContext --connection "Server=.\Seif;Database=SYNFLOX_Client;Trusted_Connection=True;TrustServerCertificate=True;"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to update Replica database!
    pause
    exit /b 1
)
echo.
echo [OK] Replica database updated successfully!
echo.

echo ============================================================
echo    Both databases are up to date!
echo ============================================================
echo.
pause
