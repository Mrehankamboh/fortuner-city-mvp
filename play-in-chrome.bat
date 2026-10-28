@echo off
rem Opens the WebGL build in Chrome via a local server (http://localhost:8080).
cd /d "%~dp0"
if not exist "Builds\WebGL\index.html" (
  echo WebGL build nahi mili. Pehle Unity menu se chalao:  Fortuner City ^> Build for Chrome ^(WebGL^)
  pause
  exit /b 1
)
where node >nul 2>nul
if errorlevel 1 (
  echo Node.js nahi mila. Free install: https://nodejs.org
  pause
  exit /b 1
)
rem open Chrome after the server has had a moment to start
start "" cmd /c "timeout /t 2 >nul & start chrome http://localhost:8080 || start http://localhost:8080"
node serve-webgl.js
