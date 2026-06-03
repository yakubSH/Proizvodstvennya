@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

dotnet publish "TariffPaymentAccounting.App\TariffPaymentAccounting.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:EnableCompressionInSingleFile=true -o "publish"
if errorlevel 1 (
    echo.
    echo Build failed.
    pause
    exit /b 1
)

echo.
echo Ready: %~dp0publish\TariffPaymentAccounting.exe
pause
