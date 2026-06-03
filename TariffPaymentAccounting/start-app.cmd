@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

if exist "publish\TariffPaymentAccounting.exe" (
    start "" "publish\TariffPaymentAccounting.exe"
    exit /b 0
)

dotnet run --project "TariffPaymentAccounting.App\TariffPaymentAccounting.App.csproj"
if errorlevel 1 pause
