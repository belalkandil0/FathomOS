@echo off
:: ================================================================
::   FATHOM OS TIME SYNC AGENT - BUILD INSTALLER
::   Builds Agent + Tray into a single installer package
:: ================================================================

title Build Installer Package

echo.
echo  ╔════════════════════════════════════════════════════════════╗
echo  ║     FATHOM OS TIME SYNC AGENT - BUILD INSTALLER            ║
echo  ╚════════════════════════════════════════════════════════════╝
echo.

set "SCRIPT_DIR=%~dp0"
set "OUTPUT_DIR=%SCRIPT_DIR%Installer"

:: Check for .NET SDK
where dotnet >nul 2>&1
if %errorLevel% neq 0 (
    echo  [X] .NET 8 SDK not found!
    echo.
    echo      Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

:: Show .NET version
echo  [OK] .NET SDK found
dotnet --version
echo.

:: Clean output directory
if exist "%OUTPUT_DIR%" (
    echo  [*] Cleaning previous build...
    rmdir /S /Q "%OUTPUT_DIR%" >nul 2>&1
)
mkdir "%OUTPUT_DIR%"

:: Build Agent
echo  [1/4] Building Time Sync Agent (Windows Service)...
dotnet publish "%SCRIPT_DIR%FathomOS.TimeSyncAgent\FathomOS.TimeSyncAgent.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT_DIR%" ^
    -v q

if %errorLevel% neq 0 (
    echo.
    echo  [X] Agent build FAILED!
    pause
    exit /b 1
)
echo        [OK] Agent built successfully

:: Build Tray
echo  [2/4] Building Tray Widget (System Tray Monitor)...
dotnet publish "%SCRIPT_DIR%FathomOS.TimeSyncAgent.Tray\FathomOS.TimeSyncAgent.Tray.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT_DIR%" ^
    -v q

if %errorLevel% neq 0 (
    echo.
    echo  [X] Tray build FAILED!
    pause
    exit /b 1
)
echo        [OK] Tray built successfully

:: Copy installer files
echo  [3/4] Copying installer files...
copy /Y "%SCRIPT_DIR%FathomOS.TimeSyncAgent\Installer\Setup.bat" "%OUTPUT_DIR%\" >nul
copy /Y "%SCRIPT_DIR%FathomOS.TimeSyncAgent\Installer\README.txt" "%OUTPUT_DIR%\" >nul
copy /Y "%SCRIPT_DIR%FathomOS.TimeSyncAgent\appsettings.json" "%OUTPUT_DIR%\" >nul

:: Cleanup unnecessary files
del "%OUTPUT_DIR%\*.pdb" 2>nul
del "%OUTPUT_DIR%\*.deps.json" 2>nul
del "%OUTPUT_DIR%\*.runtimeconfig.json" 2>nul

:: Create ZIP
echo  [4/4] Creating ZIP package...
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%SCRIPT_DIR%TimeSyncAgent_Installer.zip' -Force"

echo.
echo  ╔════════════════════════════════════════════════════════════╗
echo  ║                    BUILD COMPLETE!                         ║
echo  ╠════════════════════════════════════════════════════════════╣
echo  ║                                                            ║
echo  ║  Output: Installer\                                        ║
echo  ║  ZIP:    TimeSyncAgent_Installer.zip                       ║
echo  ║                                                            ║
echo  ╚════════════════════════════════════════════════════════════╝
echo.
echo  Contents of Installer folder:
echo  ─────────────────────────────
dir /B "%OUTPUT_DIR%"
echo.
echo  ─────────────────────────────
echo  NEXT STEPS:
echo    1. Copy the Installer folder (or ZIP) to target computers
echo    2. Users double-click Setup.bat
echo    3. Done!
echo.
pause
