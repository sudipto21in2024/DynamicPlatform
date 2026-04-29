@echo off
setlocal
title DynamicPlatform Stopper

echo ============================================================
echo   DynamicPlatform - Stop Application
echo ============================================================
echo.

:: Check for Docker
docker --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Docker is not installed or not in PATH.
    pause
    exit /b 1
)

echo Stopping and removing containers...
docker-compose down

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Failed to stop containers.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   Application stopped successfully.
echo ============================================================
echo.
pause

endlocal
