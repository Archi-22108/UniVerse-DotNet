@echo off
title UniVerse C# ASP.NET Core Server
echo ==============================================================================
echo  🏛️ UniVerse Campus Super-App - C# ASP.NET Core with ADO.NET
echo ==============================================================================
echo.
cd /d "%~dp0"
echo Starting backend server on http://localhost:5277 ...
echo Interactive Swagger documentation will be available at:
echo   http://localhost:5277/swagger
echo.
dotnet run --project UniVerse.Server
pause
