@echo off
echo PostgreSQL Distributed Cache Benchmarks
echo ========================================
echo.

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker is not running or not accessible.
    echo Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo Docker is running. Starting benchmarks...
echo.

REM Set configuration to Release for accurate benchmarks
set CONFIGURATION=Release

REM Check if specific benchmark was requested
if "%1"=="" (
    echo Running all benchmarks...
    dotnet run --configuration %CONFIGURATION%
) else (
    echo Running %1 benchmark...
    dotnet run --configuration %CONFIGURATION% -- %1
)

echo.
echo Benchmarks completed!
echo Results can be found in BenchmarkDotNet.Artifacts\results\
echo.
pause 