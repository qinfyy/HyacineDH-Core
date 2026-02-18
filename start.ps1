param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoBuild,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ProgramArgs
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

Write-Host "== HyacineCore startup ==" -ForegroundColor Cyan
Write-Host "Root: $Root"
Write-Host "Configuration: $Configuration"

function Ensure-Dir([string]$Path) {
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Host "Created directory: $Path"
    }
}

# Ensure required directories
Ensure-Dir "Config"
Ensure-Dir "Config/Database"

# Build
if (-not $NoBuild) {
    Write-Host "Building solution..."
    dotnet build HyacineCore.sln -c $Configuration -p:RestoreIgnoreFailedSources=true -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

# Start server
$exePath = Join-Path $Root "Program/bin/$Configuration/net9.0/HyacineCoreServer.exe"
if (Test-Path $exePath) {
    Write-Host "Starting: $exePath"
    & $exePath @ProgramArgs
    exit $LASTEXITCODE
}

Write-Host "Executable not found, fallback to dotnet run..."
dotnet run --project Program/Program.csproj -c $Configuration --no-build -- @ProgramArgs
exit $LASTEXITCODE
