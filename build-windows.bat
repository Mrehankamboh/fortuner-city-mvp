@echo off
REM Builds Windows Standalone via Unity batchmode.
REM Set UNITY_EDITOR to your Editor\Unity.exe if not found automatically.

setlocal
set PROJECT=%~dp0
set PROJECT=%PROJECT:~0,-1%

if not defined UNITY_EDITOR (
  for /f "delims=" %%i in ('dir /b /ad /o-n "C:\Program Files\Unity\Hub\Editor" 2^>nul') do (
    if exist "C:\Program Files\Unity\Hub\Editor\%%i\Editor\Unity.exe" (
      set "UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\%%i\Editor\Unity.exe"
      goto :found
    )
  )
)

:found
if not defined UNITY_EDITOR (
  echo Unity Editor not found. Install Unity 6 LTS via Hub, then re-run.
  echo Or set UNITY_EDITOR to full path of Unity.exe
  exit /b 1
)

echo Using: %UNITY_EDITOR%
"%UNITY_EDITOR%" -quit -batchmode -nographics -projectPath "%PROJECT%" -executeMethod FortunerCity.EditorTools.BuildWindows.Build -logFile "%PROJECT%\Builds\build.log"
echo Exit code: %ERRORLEVEL%
echo Log: %PROJECT%\Builds\build.log
endlocal
