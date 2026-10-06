@echo off
title UniVerse Campus Delivery Network
echo ========================================================
echo     Starting UniVerse Campus Delivery Network (.NET 10)
echo ========================================================
echo.
echo 1. Starting Backend Server on port 5277...
start "UniVerse Server" cmd /k "dotnet run --project UniVerse.Server"
timeout /t 4 > nul
echo.
echo 2. Starting Cloudflare Public Mobile Tunnel...
start "UniVerse Live Tunnel" cmd /k "cloudflared.exe tunnel --url http://127.0.0.1:5277"
echo.
echo ========================================================
echo  UniVerse is now LIVE for all students!
echo  Local Laptop URL : http://localhost:5277
echo  Public Mobile URL : Check the Cloudflare window above
echo ========================================================
pause
