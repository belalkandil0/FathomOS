@echo off
:: ================================================================
::   FATHOM OS TIME SYNC AGENT - ONE-CLICK SETUP
::   Installs: Background Service + System Tray Widget
::   Just double-click this file (Run as Administrator)
:: ================================================================

title Fathom OS Time Sync Agent Setup

:: Request admin if not already
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo Requesting Administrator privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
    pushd "%CD%"
    CD /D "%~dp0"

cls
echo.
echo  ╔════════════════════════════════════════════════════════════╗
echo  ║     FATHOM OS TIME SYNC AGENT - SETUP                      ║
echo  ╠════════════════════════════════════════════════════════════╣
echo  ║  This will install:                                        ║
echo  ║    1. Time Sync Agent (background service)                 ║
echo  ║    2. Tray Widget (system tray monitor) [optional]         ║
echo  ╚════════════════════════════════════════════════════════════╝
echo.

set "SERVICE_NAME=FathomOSTimeSyncAgent"
set "INSTALL_DIR=%ProgramFiles%\FathomOS\TimeSyncAgent"
set "SCRIPT_DIR=%~dp0"
set "PORT=7700"
set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"

:: Check if already installed
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
    echo  [!] Agent is already installed on this computer.
    echo.
    echo      1 = Reinstall ^(recommended for updates^)
    echo      2 = Uninstall completely
    echo      3 = Exit
    echo.
    choice /C 123 /N /M "  Select option: "
    if errorlevel 3 goto :eof
    if errorlevel 2 goto :uninstall
    if errorlevel 1 goto :reinstall
)

goto :install

:reinstall
echo.
echo  [*] Stopping existing service...
net stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul
echo  [*] Removing old service...
sc delete "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul
:: Kill tray app if running
taskkill /F /IM "FathomOS.TimeSyncAgent.Tray.exe" >nul 2>&1

:install
echo.
echo  [*] Installing Fathom OS Time Sync Agent...
echo.

:: Create install directory
echo  [1/7] Creating installation folder...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%" 2>nul
if %errorLevel% neq 0 (
    echo  [X] Failed to create folder. Check permissions.
    goto :error
)

:: Copy Agent executable
echo  [2/7] Copying Agent service...
if exist "%SCRIPT_DIR%FathomOS.TimeSyncAgent.exe" (
    copy /Y "%SCRIPT_DIR%FathomOS.TimeSyncAgent.exe" "%INSTALL_DIR%\" >nul
    if exist "%SCRIPT_DIR%appsettings.json" copy /Y "%SCRIPT_DIR%appsettings.json" "%INSTALL_DIR%\" >nul
) else (
    echo  [X] ERROR: FathomOS.TimeSyncAgent.exe not found!
    echo      Make sure Setup.bat is in the same folder as the EXE.
    goto :error
)

:: Copy Tray executable
echo  [3/7] Copying Tray widget...
set "TRAY_INSTALLED=0"
if exist "%SCRIPT_DIR%FathomOS.TimeSyncAgent.Tray.exe" (
    copy /Y "%SCRIPT_DIR%FathomOS.TimeSyncAgent.Tray.exe" "%INSTALL_DIR%\" >nul
    set "TRAY_INSTALLED=1"
    echo        [OK] Tray widget included
) else (
    echo        [--] Tray widget not found (optional)
)

:: Install service
echo  [4/7] Installing Windows service...
sc create "%SERVICE_NAME%" binPath= "\"%INSTALL_DIR%\FathomOS.TimeSyncAgent.exe\"" start= auto DisplayName= "Fathom OS Time Sync Agent" >nul 2>&1
sc description "%SERVICE_NAME%" "Enables time synchronization from Fathom OS Network Time Sync module" >nul 2>&1
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/10000/restart/30000 >nul 2>&1

:: Firewall
echo  [5/7] Configuring firewall...
netsh advfirewall firewall delete rule name="Fathom OS Time Sync Agent" >nul 2>&1
netsh advfirewall firewall add rule name="Fathom OS Time Sync Agent" dir=in action=allow protocol=tcp localport=%PORT% >nul 2>&1

:: Start service
echo  [6/7] Starting service...
net start "%SERVICE_NAME%" >nul 2>&1

:: Setup Tray auto-start
echo  [7/7] Configuring Tray widget...
if "%TRAY_INSTALLED%"=="1" (
    :: Create startup shortcut
    echo Set oWS = WScript.CreateObject("WScript.Shell") > "%temp%\createshortcut.vbs"
    echo sLinkFile = "%STARTUP_DIR%\Fathom OS Time Sync Tray.lnk" >> "%temp%\createshortcut.vbs"
    echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%temp%\createshortcut.vbs"
    echo oLink.TargetPath = "%INSTALL_DIR%\FathomOS.TimeSyncAgent.Tray.exe" >> "%temp%\createshortcut.vbs"
    echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%temp%\createshortcut.vbs"
    echo oLink.Description = "Fathom OS Time Sync Agent Tray Monitor" >> "%temp%\createshortcut.vbs"
    echo oLink.Save >> "%temp%\createshortcut.vbs"
    cscript /nologo "%temp%\createshortcut.vbs" >nul 2>&1
    del "%temp%\createshortcut.vbs" >nul 2>&1
    
    :: Launch tray now
    start "" "%INSTALL_DIR%\FathomOS.TimeSyncAgent.Tray.exe"
    echo        [OK] Tray will auto-start with Windows
)

:: Verify
echo.
sc query "%SERVICE_NAME%" | findstr "RUNNING" >nul 2>&1
if %errorLevel% equ 0 (
    echo  ╔════════════════════════════════════════════════════════════╗
    echo  ║                 INSTALLATION SUCCESSFUL!                   ║
    echo  ╠════════════════════════════════════════════════════════════╣
    echo  ║                                                            ║
    echo  ║  Agent Service: Running (auto-starts with Windows)         ║
    if "%TRAY_INSTALLED%"=="1" (
    echo  ║  Tray Widget:   Running (check system tray)                ║
    )
    echo  ║                                                            ║
    echo  ║  Port: %PORT%                                              ║
    echo  ║  Location: %INSTALL_DIR%      ║
    echo  ║                                                            ║
    echo  ║  You can now use Fathom OS Network Time Sync module        ║
    echo  ║  to discover and sync this computer.                       ║
    echo  ╚════════════════════════════════════════════════════════════╝
) else (
    echo  [!] Service installed but may not have started.
    echo      Check Windows Services (services.msc) for details.
)
goto :done

:uninstall
echo.
echo  [*] Uninstalling Fathom OS Time Sync Agent...
echo.

:: Kill tray app
echo  [1/5] Stopping Tray widget...
taskkill /F /IM "FathomOS.TimeSyncAgent.Tray.exe" >nul 2>&1

echo  [2/5] Stopping service...
net stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul

echo  [3/5] Removing service...
sc delete "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul

echo  [4/5] Removing firewall rule...
netsh advfirewall firewall delete rule name="Fathom OS Time Sync Agent" >nul 2>&1

:: Remove startup shortcut
del "%STARTUP_DIR%\Fathom OS Time Sync Tray.lnk" >nul 2>&1

echo  [5/5] Removing files...
if exist "%INSTALL_DIR%" rmdir /S /Q "%INSTALL_DIR%" 2>nul

echo.
echo  ╔════════════════════════════════════════════════════════════╗
echo  ║              UNINSTALLATION COMPLETE                       ║
echo  ╚════════════════════════════════════════════════════════════╝
goto :done

:error
echo.
echo  ╔════════════════════════════════════════════════════════════╗
echo  ║                    SETUP FAILED                            ║
echo  ╠════════════════════════════════════════════════════════════╣
echo  ║  Please check the error message above.                     ║
echo  ║  Make sure you're running as Administrator.                ║
echo  ╚════════════════════════════════════════════════════════════╝

:done
echo.
echo  Press any key to close...
pause >nul
