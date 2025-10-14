# SchemaMagic - Manual Build and Publish Guide

## Prerequisites
1. .NET 9 SDK installed
2. NuGet API key saved in `.secrets/nuget-api-key.txt`
3. Git repository with clean working directory

---

## Quick Publish (PowerShell)

```powershell
# Read API key from file
$API_KEY = Get-Content ".secrets/nuget-api-key.txt" -Raw
$API_KEY = $API_KEY.Trim()

# Get current version
nbgv get-version

# Build and pack
dotnet restore
dotnet build --configuration Release
dotnet pack SchemaMagic/SchemaMagic.csproj --configuration Release --output ./nupkg

# Publish to NuGet
dotnet nuget push ./nupkg/*.nupkg `
  --api-key $API_KEY `
  --source https://api.nuget.org/v3/index.json `
  --skip-duplicate
```

---

## Step-by-Step Manual Process

### 1. Clean and Restore
```powershell
# Clean previous builds
dotnet clean

# Restore dependencies
dotnet restore
```

### 2. Run Tests
```powershell
# Run all tests
dotnet test --configuration Release
```

### 3. Build Release
```powershell
# Build in Release mode
dotnet build --configuration Release
```

### 4. Check Version
```powershell
# Install nbgv if not already installed
dotnet tool install -g nbgv

# Check current version
nbgv get-version

# Get version details
nbgv get-version -v SemVer2
```

### 5. Pack NuGet Package
```powershell
# Create NuGet package
dotnet pack SchemaMagic/SchemaMagic.csproj `
  --configuration Release `
  --output ./nupkg `
  --no-build
```

### 6. Inspect Package (Optional)
```powershell
# List generated packages
Get-ChildItem ./nupkg

# Verify package contents
nuget verify -All ./nupkg/*.nupkg
```

### 7. Publish to NuGet
```powershell
# Load API key
$API_KEY = (Get-Content ".secrets/nuget-api-key.txt").Trim()

# Publish package
dotnet nuget push ./nupkg/*.nupkg `
  --api-key $API_KEY `
  --source https://api.nuget.org/v3/index.json `
  --skip-duplicate

# Or publish specific version
dotnet nuget push "./nupkg/SchemaMagic.1.0.21.nupkg" `
  --api-key $API_KEY `
  --source https://api.nuget.org/v3/index.json
```

### 8. Create Git Tag (After Publishing)
```powershell
# Get the version
$VERSION = nbgv get-version -v SemVer2

# Create annotated tag
git tag -a "v$VERSION" -m "Release v$VERSION"

# Push tag to GitHub
git push origin "v$VERSION"
```

---

## Test Package Locally Before Publishing

### Install from Local Package
```powershell
# Install from local nupkg
dotnet tool install -g SchemaMagic --add-source ./nupkg --version 1.0.21

# Test the tool
schemamagic --version
schemamagic --help

# Uninstall after testing
dotnet tool uninstall -g SchemaMagic
```

---

## Publish to Test Feed (Optional)

If you have a test NuGet feed (like Azure Artifacts):

```powershell
# Add test source
dotnet nuget add source "https://pkgs.dev.azure.com/yourorg/_packaging/test/nuget/v3/index.json" `
  --name "TestFeed" `
  --username "user" `
  --password "pat_token" `
  --store-password-in-clear-text

# Push to test feed
dotnet nuget push ./nupkg/*.nupkg `
  --api-key "az" `
  --source "TestFeed"
```

---

## Troubleshooting

### Package Already Exists
```powershell
# Use --skip-duplicate to ignore if version exists
dotnet nuget push ./nupkg/*.nupkg `
  --api-key $API_KEY `
  --source https://api.nuget.org/v3/index.json `
  --skip-duplicate
```

### Invalid API Key
1. Verify key in `.secrets/nuget-api-key.txt` has no extra spaces
2. Check key hasn't expired at https://www.nuget.org/account/apikeys
3. Ensure key has "Push" permissions

### Version Already Published
- NuGet.org does NOT allow re-publishing the same version
- Increment version in `version.json` or make a new commit
- Use `--skip-duplicate` to continue if version exists

### Clean Build
```powershell
# Full clean and rebuild
dotnet clean
Remove-Item -Recurse -Force ./nupkg
dotnet restore
dotnet build --configuration Release
dotnet pack SchemaMagic/SchemaMagic.csproj --configuration Release --output ./nupkg
```

---

## Automated Script

Save this as `publish-manual.ps1`:

```powershell
#Requires -Version 7.0

param(
    [switch]$DryRun,
    [switch]$SkipTests,
    [string]$ApiKeyFile = ".secrets/nuget-api-key.txt"
)

Write-Host "?? SchemaMagic Manual Publish Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check API key file exists
if (-not (Test-Path $ApiKeyFile)) {
    Write-Host "? API key file not found: $ApiKeyFile" -ForegroundColor Red
    exit 1
}

# Load API key
$API_KEY = (Get-Content $ApiKeyFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($API_KEY)) {
    Write-Host "? API key file is empty" -ForegroundColor Red
    exit 1
}

# Get version
Write-Host "?? Getting version..." -ForegroundColor Yellow
$VERSION = nbgv get-version -v SemVer2
Write-Host "? Version: $VERSION" -ForegroundColor Green

# Clean
Write-Host "?? Cleaning..." -ForegroundColor Yellow
dotnet clean
Remove-Item -Recurse -Force ./nupkg -ErrorAction SilentlyContinue

# Restore
Write-Host "?? Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Run tests
if (-not $SkipTests) {
    Write-Host "?? Running tests..." -ForegroundColor Yellow
    dotnet test --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Tests failed" -ForegroundColor Red
        exit 1
    }
}

# Build
Write-Host "?? Building..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

# Pack
Write-Host "?? Packing NuGet package..." -ForegroundColor Yellow
dotnet pack SchemaMagic/SchemaMagic.csproj `
    --configuration Release `
    --output ./nupkg `
    --no-build

# List packages
Write-Host "?? Generated packages:" -ForegroundColor Yellow
Get-ChildItem ./nupkg/*.nupkg | ForEach-Object {
    Write-Host "   $_" -ForegroundColor Cyan
}

if ($DryRun) {
    Write-Host "?? DRY RUN - Skipping publish" -ForegroundColor Yellow
    Write-Host "? Build successful! Package ready at ./nupkg/" -ForegroundColor Green
    exit 0
}

# Publish
Write-Host "?? Publishing to NuGet.org..." -ForegroundColor Yellow
dotnet nuget push ./nupkg/*.nupkg `
    --api-key $API_KEY `
    --source https://api.nuget.org/v3/index.json `
    --skip-duplicate

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Published successfully!" -ForegroundColor Green
    Write-Host "?? Package: https://www.nuget.org/packages/SchemaMagic/$VERSION" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "?? Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Create git tag: git tag -a v$VERSION -m 'Release v$VERSION'" -ForegroundColor White
    Write-Host "   2. Push tag: git push origin v$VERSION" -ForegroundColor White
    Write-Host "   3. Wait 5-10 minutes for NuGet indexing" -ForegroundColor White
    Write-Host "   4. Test: dotnet tool update -g SchemaMagic" -ForegroundColor White
} else {
    Write-Host "? Publish failed" -ForegroundColor Red
    exit 1
}
```

Usage:
```powershell
# Test build without publishing
.\publish-manual.ps1 -DryRun

# Skip tests and publish
.\publish-manual.ps1 -SkipTests

# Full build and publish
.\publish-manual.ps1
```

---

## Verification After Publishing

```powershell
# Wait 5-10 minutes for NuGet indexing, then:

# Search for package
dotnet tool search SchemaMagic

# Install latest version
dotnet tool install -g SchemaMagic

# Or update existing
dotnet tool update -g SchemaMagic

# Verify installation
schemamagic --version
```

---

## Best Practices

1. ? **Always test locally first** with `--add-source ./nupkg`
2. ? **Run tests before publishing** to ensure quality
3. ? **Use semantic versioning** (MAJOR.MINOR.PATCH)
4. ? **Create git tag after successful publish**
5. ? **Document changes** in CHANGELOG.md
6. ? **Never commit API keys** - keep them in .gitignored files
7. ? **Use --skip-duplicate** to avoid errors on re-runs
8. ? **Wait for indexing** before testing installation (5-10 min)

---

## Links

- **NuGet Package**: https://www.nuget.org/packages/SchemaMagic
- **API Keys Management**: https://www.nuget.org/account/apikeys
- **NuGet Documentation**: https://docs.microsoft.com/en-us/nuget/
- **Nerdbank.GitVersioning**: https://github.com/dotnet/Nerdbank.GitVersioning
