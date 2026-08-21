@echo off
setlocal EnableDelayedExpansion
title Vantage - Launcher

rem ---------------------------------------------------------------
rem  Starts the Vantage API and the frontend dev server, waits for
rem  both to answer, then opens the app in the default browser.
rem  Each server runs in its own window; closing that window stops it.
rem
rem  Ports are pinned here AND in frontend/vite.config.ts (strictPort)
rem  AND in the API's CORS policy in Program.cs. Change one, change all.
rem ---------------------------------------------------------------

set "ROOT=%~dp0"
set "BACKEND_DIR=%ROOT%backend"
set "FRONTEND_DIR=%ROOT%frontend"
set "BACKEND_PORT=5199"
set "FRONTEND_PORT=5180"
set "APP_URL=http://localhost:%FRONTEND_PORT%"

echo.
echo === Vantage ===
echo.

rem --- prerequisites ---------------------------------------------
where dotnet >nul 2>&1 || (
  echo [ERROR] dotnet SDK not found on PATH. Install .NET 9 SDK: https://dotnet.microsoft.com/download
  goto :fail
)
where npm >nul 2>&1 || (
  echo [ERROR] npm not found on PATH. Install Node.js: https://nodejs.org
  goto :fail
)

rem --- ports already in use? -------------------------------------
rem  Nothing is started until both are clear, so a half-up app never
rem  gets a browser window opened on it.
call :is_port_open %BACKEND_PORT% && (
  call :report_holder %BACKEND_PORT% "API"
  goto :fail
)
call :is_port_open %FRONTEND_PORT% && (
  call :report_holder %FRONTEND_PORT% "frontend"
  goto :fail
)

rem --- frontend dependencies -------------------------------------
if not exist "%FRONTEND_DIR%\node_modules" (
  echo [setup] Installing frontend dependencies ^(first run^)...
  pushd "%FRONTEND_DIR%"
  call npm install
  set "NPM_EXIT=!errorlevel!"
  popd
  if not "!NPM_EXIT!"=="0" (
    echo [ERROR] npm install failed.
    goto :fail
  )
)

rem --- launch ----------------------------------------------------
echo [start] API      -^> http://localhost:%BACKEND_PORT%
start "Vantage API" cmd /k "cd /d "%BACKEND_DIR%" && dotnet run --project src\Vantage.Api -- --urls http://localhost:%BACKEND_PORT%"

echo [start] Frontend -^> %APP_URL%
start "Vantage Frontend" cmd /k "cd /d "%FRONTEND_DIR%" && npm run dev"

call :wait_for_port %BACKEND_PORT% "API" 90 || goto :fail
call :wait_for_port %FRONTEND_PORT% "frontend" 60 || goto :fail

echo.
echo [ready] Opening %APP_URL%
start "" "%APP_URL%"
echo.
echo Both servers run in their own windows. Close those windows to stop them.
echo.
ping -n 6 127.0.0.1 >nul
exit /b 0

rem ---------------------------------------------------------------
rem  :is_port_open PORT      -> exit 0 if something is listening
rem ---------------------------------------------------------------
:is_port_open
powershell -NoProfile -Command "if (Get-NetTCPConnection -State Listen -LocalPort %1 -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 (exit /b 1) else (exit /b 0)

rem ---------------------------------------------------------------
rem  :report_holder PORT LABEL   -> name the process squatting on it
rem ---------------------------------------------------------------
:report_holder
echo [ERROR] Port %~1 ^(%~2^) is already in use by:
powershell -NoProfile -Command "Get-NetTCPConnection -State Listen -LocalPort %~1 -ErrorAction SilentlyContinue | Select-Object -Expand OwningProcess -Unique | ForEach-Object { $p = Get-Process -Id $_ -ErrorAction SilentlyContinue; if ($p) { '          PID {0}  {1}' -f $_, $p.ProcessName } else { '          PID {0}' -f $_ } }"
echo         Stop it with:  taskkill /PID ^<pid^> /F
echo         Nothing was started.
exit /b 0

rem ---------------------------------------------------------------
rem  :wait_for_port PORT LABEL TIMEOUT_SECONDS
rem ---------------------------------------------------------------
:wait_for_port
set "WP_PORT=%~1"
set "WP_LABEL=%~2"
set /a WP_LEFT=%~3
<nul set /p "=[wait] Waiting for %WP_LABEL% on port %WP_PORT% "
:wait_loop
call :is_port_open %WP_PORT% && (
  echo  ok
  exit /b 0
)
if %WP_LEFT% leq 0 (
  echo  timeout
  echo [ERROR] %WP_LABEL% did not start. Check its window for errors.
  exit /b 1
)
<nul set /p "=."
ping -n 2 127.0.0.1 >nul
set /a WP_LEFT-=1
goto :wait_loop

:fail
echo.
pause
exit /b 1
