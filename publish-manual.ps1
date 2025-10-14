#Requires -Version 7.0

<#
.SYNOPSIS
    Manual NuGet publish script for SchemaMagic

.DESCRIPTION
    Builds, packs, and publishes SchemaMagic to NuGet.org
    
.PARAMETER DryRun
    Test build without publishing

.PARAMETER SkipTests
    Skip running tests before publishing

.PARAMETER ApiKeyFile
    Path to file containing NuGet API key (default: .secrets/nuget-api-key.txt)

.EXAMPLE
    .\publish-manual.ps1
    Full build, test, and publish

.EXAMPLE
    .\publish-manual.ps1 -DryRun
    Test build without publishing

.EXAMPLE
    .\publish-manual.ps1 -SkipTests
    Skip tests and publish
#>

param(
    [switch]$DryRun,
    [switch]$SkipTests,
    [string]$ApiKeyFile = ".secrets/nuget-api-key.txt"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "?? SchemaMagic Manual Publish Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if nbgv is installed
try {
    $null = nbgv --version
} catch {
    Write-Host "? Nerdbank.GitVersioning (nbgv) not installed" -ForegroundColor Red
    Write-Host "?? Installing nbgv..." -ForegroundColor Yellow
    dotnet tool install -g nbgv
}

# Check API key file exists
if (-not (Test-Path $ApiKeyFile)) {
    Write-Host "? API key file not found: $ApiKeyFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Create the file and add your NuGet API key:" -ForegroundColor Yellow
    Write-Host "   1. Get key from: https://www.nuget.org/account/apikeys" -ForegroundColor White
    Write-Host "   2. Save it to: $ApiKeyFile" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Load API key
$API_KEY = (Get-Content $ApiKeyFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($API_KEY)) {
    Write-Host "? API key file is empty: $ApiKeyFile" -ForegroundColor Red
    exit 1
}

Write-Host "? API key loaded from $ApiKeyFile" -ForegroundColor Green

# Get version
Write-Host ""
Write-Host "?? Getting version..." -ForegroundColor Yellow
$VERSION = nbgv get-version -v SemVer2
$SIMPLE_VERSION = nbgv get-version -v SimpleVersion
Write-Host "? Version: $VERSION" -ForegroundColor Green
Write-Host "   Simple: $SIMPLE_VERSION" -ForegroundColor Cyan

# Check git status
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host ""
    Write-Host "??  Warning: You have uncommitted changes" -ForegroundColor Yellow
    Write-Host "$gitStatus" -ForegroundColor Gray
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne "y") {
        Write-Host "? Cancelled" -ForegroundColor Red
        exit 1
    }
}

# Clean
Write-Host ""
Write-Host "?? Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --verbosity quiet
Remove-Item -Recurse -Force ./nupkg -ErrorAction SilentlyContinue
Write-Host "? Clean complete" -ForegroundColor Green

# Restore
Write-Host ""
Write-Host "?? Restoring dependencies..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
Write-Host "? Dependencies restored" -ForegroundColor Green

# Run tests
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "?? Running tests..." -ForegroundColor Yellow
    dotnet test --configuration Release --verbosity quiet --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Tests failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "? All tests passed" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "??  Skipping tests" -ForegroundColor Yellow
}

# Build
Write-Host ""
Write-Host "?? Building Release configuration..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore --verbosity quiet --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "? Build successful" -ForegroundColor Green

# Pack
Write-Host ""
Write-Host "?? Creating NuGet package..." -ForegroundColor Yellow
dotnet pack SchemaMagic/SchemaMagic.csproj `
    --configuration Release `
    --output ./nupkg `
    --no-build `
    --verbosity quiet `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Pack failed" -ForegroundColor Red
    exit 1
}

# List generated packages
Write-Host "? Package created" -ForegroundColor Green
Write-Host ""
Write-Host "?? Generated packages:" -ForegroundColor Cyan
Get-ChildItem ./nupkg/*.nupkg | ForEach-Object {
    $size = [math]::Round($_.Length / 1KB, 2)
    Write-Host "   $($_.Name) ($size KB)" -ForegroundColor White
}

if ($DryRun) {
    Write-Host ""
    Write-Host "?? DRY RUN - Skipping publish" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "? Build successful! Package ready at ./nupkg/" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? To publish for real, run without -DryRun:" -ForegroundColor Yellow
    Write-Host "   .\publish-manual.ps1" -ForegroundColor White
    Write-Host ""
    exit 0
}

# Confirm publish
Write-Host ""
Write-Host "??  About to publish version $VERSION to NuGet.org" -ForegroundColor Yellow
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne "y") {
    Write-Host "? Cancelled" -ForegroundColor Red
    exit 1
}

# Publish
Write-Host ""
Write-Host "?? Publishing to NuGet.org..." -ForegroundColor Yellow
dotnet nuget push ./nupkg/*.nupkg `
    --api-key $API_KEY `
    --source https://api.nuget.org/v3/index.json `
    --skip-duplicate

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? Published successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Package URL: https://www.nuget.org/packages/SchemaMagic/$SIMPLE_VERSION" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "?? Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Wait 5-10 minutes for NuGet indexing" -ForegroundColor White
    Write-Host "   2. Create git tag:" -ForegroundColor White
    Write-Host "      git tag -a v$VERSION -m 'Release v$VERSION'" -ForegroundColor Cyan
    Write-Host "   3. Push tag to GitHub:" -ForegroundColor White
    Write-Host "      git push origin v$VERSION" -ForegroundColor Cyan
    Write-Host "   4. Test installation:" -ForegroundColor White
    Write-Host "      dotnet tool update -g SchemaMagic" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "? Publish failed with exit code $LASTEXITCODE" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "   • API key expired or invalid" -ForegroundColor White
    Write-Host "   • Version already published (NuGet doesn't allow overwrites)" -ForegroundColor White
    Write-Host "   • Network connectivity issues" -ForegroundColor White
    Write-Host ""
    exit 1
}
