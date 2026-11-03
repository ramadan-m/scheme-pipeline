@echo off
rem One-click local run: Mongo (Docker), API on :5080, web on :5173, then opens the browser.
rem Close the two windows it opens to stop the app; Mongo keeps running (docker compose stop to stop it).
cd /d "%~dp0"

docker info >nul 2>&1
if errorlevel 1 (
  echo Docker isn't running. Start Docker Desktop, then run this again.
  pause
  exit /b 1
)

docker compose up -d mongo
if errorlevel 1 (
  pause
  exit /b 1
)

if not exist web\node_modules (
  pushd web
  call npm ci
  popd
)

start "Scheme Pipeline API" cmd /k dotnet run --project api\SchemePipeline.Api
start "Scheme Pipeline web" cmd /k "cd web && npm run dev"

echo Waiting for the API and web app to start...
set /a tries=0
:wait
set /a tries+=1
if %tries% gtr 90 (
  echo Still not up after 90 seconds. Check the two windows for errors.
  pause
  exit /b 1
)
timeout /t 1 /nobreak >nul
curl -s -o nul http://localhost:5080/health || goto wait
curl -s -o nul http://localhost:5173 || goto wait

start "" http://localhost:5173
echo Running: web http://localhost:5173  API http://localhost:5080
