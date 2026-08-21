#Requires -Version 5.1
<#
.SYNOPSIS
    One-time local setup for Dashboard: creates the Postgres database, stores
    the connection string as a user secret, restores/builds the backend,
    generates and applies the EF Core migration, and installs frontend
    dependencies.

.DESCRIPTION
    Safe to re-run: each step checks whether it's already done and skips if so.
    Run this from the repo root: .\setup.ps1
#>

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Test-CommandExists($name) {
    return [bool](Get-Command $name -ErrorAction SilentlyContinue)
}

$repoRoot = $PSScriptRoot
$backendDir = Join-Path $repoRoot "backend"
$apiDir = Join-Path $backendDir "src\Vantage.Api"
$infrastructureDir = Join-Path $backendDir "src\Vantage.Infrastructure"
$frontendDir = Join-Path $repoRoot "frontend"

# --- Prerequisite checks -----------------------------------------------------

Write-Step "Checking prerequisites"

if (-not (Test-CommandExists "dotnet")) {
    throw ".NET SDK not found. Install the .NET 9 SDK first: https://dotnet.microsoft.com/download/dotnet/9.0"
}

if (-not (Test-CommandExists "psql") -or -not (Test-CommandExists "createdb")) {
    throw "PostgreSQL command-line tools not found on PATH. Install PostgreSQL first: https://www.postgresql.org/download/"
}

if (-not (Test-CommandExists "node") -or -not (Test-CommandExists "npm")) {
    throw "Node.js not found. Install Node 20+ first: https://nodejs.org"
}

Write-Host "All prerequisites found."

# --- Postgres credentials ----------------------------------------------------

Write-Step "Postgres connection details"

$pgUser = Read-Host "Postgres username (the one you connect to psql with)"
$securePassword = Read-Host "Postgres password" -AsSecureString
$pgPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))

$databaseName = "vantage_dev"
$connectionString = "Host=localhost;Database=$databaseName;Username=$pgUser;Password=$pgPassword"

# --- Create the database (skip if it already exists) ------------------------

Write-Step "Creating database '$databaseName' (skipping if it already exists)"

$env:PGPASSWORD = $pgPassword
$dbExists = (psql -U $pgUser -h localhost -lqt 2>$null) -match "^\s*$databaseName\s*\|"
if ($dbExists) {
    Write-Host "Database '$databaseName' already exists, skipping."
} else {
    createdb -U $pgUser -h localhost $databaseName
    Write-Host "Database '$databaseName' created."
}

# --- Store the connection string as a user secret ---------------------------

Write-Step "Storing the connection string via dotnet user-secrets"

Push-Location $apiDir
dotnet user-secrets set "ConnectionStrings:Vantage" $connectionString
Pop-Location

# --- Restore and build --------------------------------------------------------

Write-Step "Restoring and building the backend"

Push-Location $backendDir
dotnet restore
dotnet build
Pop-Location

# --- EF Core tool + migration -------------------------------------------------

Write-Step "Checking for the dotnet-ef tool"

if (-not (Test-CommandExists "dotnet-ef")) {
    Write-Host "Installing dotnet-ef globally..."
    dotnet tool install --global dotnet-ef
} else {
    Write-Host "dotnet-ef already installed."
}

$migrationsDir = Join-Path $infrastructureDir "Migrations"
Write-Step "Creating the EF Core migration (skipping if one already exists)"

if (Test-Path $migrationsDir) {
    Write-Host "A migration already exists in $migrationsDir, skipping 'migrations add'."
} else {
    Push-Location $backendDir
    dotnet ef migrations add InitialCreate --project src/Vantage.Infrastructure --startup-project src/Vantage.Api
    Pop-Location
    Write-Host "Migration created."
}

Write-Step "Applying the migration to the database"

Push-Location $backendDir
dotnet ef database update --project src/Vantage.Infrastructure --startup-project src/Vantage.Api
Pop-Location

# --- Frontend dependencies -----------------------------------------------------

Write-Step "Installing frontend dependencies"

Push-Location $frontendDir
npm install
Pop-Location

# --- Done -----------------------------------------------------------------------

Write-Step "Setup complete"

Write-Host @"

Everything is set up. To run Dashboard:

  Terminal 1 (backend):
    dotnet run --project backend/src/Vantage.Api

  Terminal 2 (frontend):
    cd frontend
    npm run dev

Then open http://localhost:5173 -- the API will auto-migrate and seed
sample data on its first run in Development.

"@ -ForegroundColor Green
