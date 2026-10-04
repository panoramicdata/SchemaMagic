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

# Set console encoding to UTF-8 to display emojis correctly
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Information "" -InformationAction Continue
Write-Information "?? SchemaMagic Manual Publish Script" -InformationAction Continue
Write-Information "=====================================" -InformationAction Continue
Write-Information "" -InformationAction Continue

# Check if nbgv is installed
try {
    $null = nbgv --version
} catch {
    Write-Information "? Nerdbank.GitVersioning (nbgv) not installed" -InformationAction Continue
    Write-Information "?? Installing nbgv..." -InformationAction Continue
    dotnet tool install -g nbgv
}

# Check API key file exists
if (-not (Test-Path $ApiKeyFile)) {
    Write-Information "? API key file not found: $ApiKeyFile" -InformationAction Continue
    Write-Information "" -InformationAction Continue
    Write-Information "?? Create the file and add your NuGet API key:" -InformationAction Continue
    Write-Information "   1. Get key from: https://www.nuget.org/account/apikeys" -InformationAction Continue
    Write-Information "   2. Save it to: $ApiKeyFile" -InformationAction Continue
    Write-Information "" -InformationAction Continue
    exit 1
}

# Load API key
$API_KEY = (Get-Content $ApiKeyFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($API_KEY)) {
    Write-Information "? API key file is empty: $ApiKeyFile" -InformationAction Continue
    exit 1
}

Write-Information "? API key loaded from $ApiKeyFile" -InformationAction Continue

# Get version
Write-Information "" -InformationAction Continue
Write-Information "?? Getting version..." -InformationAction Continue
$VERSION = nbgv get-version -v SemVer2
$SIMPLE_VERSION = nbgv get-version -v SimpleVersion
Write-Information "? Version: $VERSION" -InformationAction Continue
Write-Information "   Simple: $SIMPLE_VERSION" -InformationAction Continue

# Check git status
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Information "" -InformationAction Continue
    Write-Information "??  Warning: You have uncommitted changes" -InformationAction Continue
    Write-Information "$gitStatus" -InformationAction Continue
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne "y") {
        Write-Information "? Cancelled" -InformationAction Continue
        exit 1
    }
}

# Clean
Write-Information "" -InformationAction Continue
Write-Information "?? Cleaning previous builds..." -InformationAction Continue
dotnet clean --verbosity quiet
Remove-Item -Recurse -Force ./nupkg -ErrorAction SilentlyContinue
Write-Information "? Clean complete" -InformationAction Continue

# Restore
Write-Information "" -InformationAction Continue
Write-Information "?? Restoring dependencies..." -InformationAction Continue
dotnet restore --verbosity quiet
Write-Information "? Dependencies restored" -InformationAction Continue

# Run tests
if (-not $SkipTests) {
    Write-Information "" -InformationAction Continue
    Write-Information "?? Running tests..." -InformationAction Continue
    dotnet test --configuration Release --verbosity quiet --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Information "? Tests failed" -InformationAction Continue
        exit 1
    }
    Write-Information "? All tests passed" -InformationAction Continue
} else {
    Write-Information "" -InformationAction Continue
    Write-Information "??  Skipping tests" -InformationAction Continue
}

# Build
Write-Information "" -InformationAction Continue
Write-Information "?? Building Release configuration..." -InformationAction Continue
dotnet build --configuration Release --no-restore --verbosity quiet --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Information "? Build failed" -InformationAction Continue
    exit 1
}
Write-Information "? Build successful" -InformationAction Continue

# Pack
Write-Information "" -InformationAction Continue
Write-Information "?? Creating NuGet package..." -InformationAction Continue
dotnet pack SchemaMagic/SchemaMagic.csproj `
    --configuration Release `
    --output ./nupkg `
    --no-build `
    --verbosity quiet `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Information "? Pack failed" -InformationAction Continue
    exit 1
}

# List generated packages
Write-Information "? Package created" -InformationAction Continue
Write-Information "" -InformationAction Continue
Write-Information "?? Generated packages:" -InformationAction Continue
Get-ChildItem ./nupkg/*.nupkg | ForEach-Object {
    $size = [math]::Round($_.Length / 1KB, 2)
    Write-Information "   $($_.Name) ($size KB)" -InformationAction Continue
}

if ($DryRun) {
    Write-Information "" -InformationAction Continue
    Write-Information "?? DRY RUN - Skipping publish" -InformationAction Continue
    Write-Information "" -InformationAction Continue
    Write-Information "? Build successful! Package ready at ./nupkg/" -InformationAction Continue
    Write-Information "" -InformationAction Continue
    Write-Information "?? To publish for real, run without -DryRun:" -InformationAction Continue
    Write-Information "   .\publish-manual.ps1" -InformationAction Continue
    Write-Information "" -InformationAction Continue
    exit 0
}

# Confirm publish
Write-Information "" -InformationAction Continue
Write-Information "??  About to publish version $VERSION to NuGet.org" -InformationAction Continue
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne "y") {
    Write-Information "? Cancelled" -InformationAction Continue
    exit 1
}

# Publish
Write-Information "" -InformationAction Continue
Write-Information "Publishing to NuGet.org..." -InformationAction Continue

# Get the actual nupkg file path
$nupkgFiles = Get-ChildItem ./nupkg/*.nupkg -ErrorAction Stop
if ($nupkgFiles.Count -eq 0) {
    Write-Information "❌ No .nupkg files found in ./nupkg/" -InformationAction Continue
    exit 1
}

Write-Information "Found $($nupkgFiles.Count) package(s) to publish" -InformationAction Continue

foreach ($pkg in $nupkgFiles) {
    Write-Information "   Publishing: $($pkg.Name)" -InformationAction Continue
    dotnet nuget push $pkg.FullName `
        --api-key $API_KEY `
        --source https://api.nuget.org/v3/index.json `
        --skip-duplicate
    
    if ($LASTEXITCODE -ne 0) {
        Write-Information "" -InformationAction Continue
        Write-Information "❌ Publish failed for $($pkg.Name) with exit code $LASTEXITCODE" -InformationAction Continue
        Write-Information "" -InformationAction Continue
        Write-Information "Common issues:" -InformationAction Continue
        Write-Information "   • API key expired or invalid" -InformationAction Continue
        Write-Information "   • Version already published (NuGet doesn't allow overwrites)" -InformationAction Continue
        Write-Information "   • Network connectivity issues" -InformationAction Continue
        Write-Information "" -InformationAction Continue
        exit 1
    }
}

Write-Information "" -InformationAction Continue
Write-Information "? Published successfully!" -InformationAction Continue
Write-Information "" -InformationAction Continue
Write-Information "?? Package URL: https://www.nuget.org/packages/SchemaMagic/$SIMPLE_VERSION" -InformationAction Continue
Write-Information "" -InformationAction Continue
Write-Information "?? Next steps:" -InformationAction Continue
Write-Information "   1. Wait 5-10 minutes for NuGet indexing" -InformationAction Continue
Write-Information "   2. Create git tag:" -InformationAction Continue
Write-Information "      git tag -a v$VERSION -m 'Release v$VERSION'" -InformationAction Continue
Write-Information "   3. Push tag to GitHub:" -InformationAction Continue
Write-Information "      git push origin v$VERSION" -InformationAction Continue
Write-Information "   4. Test installation:" -InformationAction Continue
Write-Information "      dotnet tool update -g SchemaMagic" -InformationAction Continue
Write-Information "" -InformationAction Continue
