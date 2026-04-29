@echo off
setlocal
title DynamicPlatform Runner

echo ============================================================
echo   DynamicPlatform - Start Application
echo ============================================================
echo.

:: Check for Docker
docker --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Docker is not installed or not in PATH.
    echo Please install Docker Desktop to run the application.
    pause
    exit /b 1
)

:: Confirm selection
echo Starting services via Docker Compose...
echo This will build and start the Database, API, and Studio.
echo.

:: Run Docker Compose
docker-compose up --build -d

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Failed to start Docker containers.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   Application started successfully in background!
echo ============================================================
echo.
echo   Frontend (Studio): http://localhost:4200
echo   Backend (API):      http://localhost:5018/swagger
echo.
echo   To see logs, run:    docker-compose logs -f
echo   To stop, run:        docker-compose down
echo ============================================================
echo.

:: Open browser automatically after a short delay
echo Waiting for services to initialize...
timeout /t 10 /nobreak >nul
echo Opening DynamicPlatform Studio...
start http://localhost:4200

endlocal
