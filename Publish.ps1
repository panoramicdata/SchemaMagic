#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automates version bumping and publishing of SchemaMagic to NuGet.

.DESCRIPTION
    This script uses Nerdbank.GitVersioning (nbgv) to automatically determine the version,
    build the package, create a git tag, and optionally publish to NuGet.

.PARAMETER PublishToNuGet
    If specified, publishes the package to NuGet.org after building.

.PARAMETER ApiKey
    NuGet API key for publishing. Can also be set via NUGET_API_KEY environment variable.

.PARAMETER DryRun
    If specified, performs all steps except actual publishing and git push.

.EXAMPLE
    .\Publish.ps1
    # Builds package and creates local tag

.EXAMPLE
    .\Publish.ps1 -PublishToNuGet -ApiKey "your-api-key"
    # Builds, tags, and publishes to NuGet

.EXAMPLE
    .\Publish.ps1 -DryRun
    # Simulates the publish process without making changes
#>

[CmdletBinding()]
param(
    [switch]$PublishToNuGet,
    [string]$ApiKey,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Header { Write-Host "?? $args" -ForegroundColor Cyan }
function Write-Success { Write-Host "? $args" -ForegroundColor Green }
function Write-Info { Write-Host "??  $args" -ForegroundColor Blue }
function Write-Warning { Write-Host "??  $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "? $args" -ForegroundColor Red }

Write-Header "SchemaMagic Publishing Script"
Write-Host "==========================================" -ForegroundColor Cyan

# Check if nbgv is installed
Write-Info "Checking for Nerdbank.GitVersioning CLI..."
$nbgvInstalled = $null -ne (Get-Command nbgv -ErrorAction SilentlyContinue)

if (-not $nbgvInstalled) {
    Write-Warning "nbgv CLI not found. Installing..."
    dotnet tool install -g nbgv
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install nbgv CLI"
        exit 1
    }
    Write-Success "nbgv CLI installed successfully"
}

# Get version information from nbgv
Write-Info "Determining version from git history..."
$versionInfo = nbgv get-version -f json | ConvertFrom-Json

if (-not $versionInfo) {
    Write-Error "Failed to get version information from nbgv"
    exit 1
}

$version = $versionInfo.SimpleVersion
$fullVersion = $versionInfo.SemVer2
$gitHeight = $versionInfo.VersionHeight

Write-Success "Version determined: $fullVersion"
Write-Info "  Major.Minor: $($versionInfo.MajorMinorVersion) (from version.json)"
Write-Info "  Patch (Git Height): $gitHeight"
Write-Info "  Full Version: $fullVersion"

# Check for uncommitted changes
Write-Info "Checking for uncommitted changes..."
$gitStatus = git status --porcelain
if ($gitStatus -and -not $DryRun) {
    Write-Error "Uncommitted changes detected. Please commit or stash your changes first."
    Write-Host "Run 'git status' to see uncommitted changes."
    exit 1
}
Write-Success "Working directory is clean"

# Create tag name
$tagName = "v$version"
Write-Info "Tag will be: $tagName"

# Check if tag already exists
$existingTag = git tag -l $tagName
if ($existingTag) {
    Write-Warning "Tag $tagName already exists locally"
    $remoteTag = git ls-remote --tags origin $tagName
    if ($remoteTag) {
        Write-Error "Tag $tagName already exists on remote. Version has already been published."
        Write-Info "To publish a new version, update version.json or make additional commits."
        exit 1
    }
}

# Clean previous builds
Write-Info "Cleaning previous builds..."
if (Test-Path "SchemaMagic/bin") {
    Remove-Item "SchemaMagic/bin" -Recurse -Force
}
if (Test-Path "SchemaMagic/obj") {
    Remove-Item "SchemaMagic/obj" -Recurse -Force
}
if (Test-Path "SchemaMagic/nupkg") {
    Remove-Item "SchemaMagic/nupkg" -Recurse -Force
}
Write-Success "Build directories cleaned"

# Build the package
Write-Header "Building Package"
Write-Info "Building SchemaMagic v$fullVersion..."

dotnet pack SchemaMagic/SchemaMagic.csproj `
    -c Release `
    -o SchemaMagic/nupkg `
    /p:PackageVersion=$fullVersion `
    /p:Version=$fullVersion

if ($LASTEXITCODE -ne 0) {
    Write-Error "Package build failed"
    exit 1
}

Write-Success "Package built successfully"

# List generated packages
$packages = Get-ChildItem "SchemaMagic/nupkg" -Filter "*.nupkg"
Write-Info "Generated packages:"
foreach ($pkg in $packages) {
    $sizeKB = [math]::Round($pkg.Length / 1KB, 2)
    Write-Host "  ?? $($pkg.Name) ($sizeKB KB)" -ForegroundColor White
}

# Create git tag
if ($DryRun) {
    Write-Warning "[DRY RUN] Would create tag: $tagName"
} else {
    Write-Info "Creating git tag: $tagName..."
    git tag -a $tagName -m "Release version $version"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create git tag"
        exit 1
    }
    Write-Success "Git tag created: $tagName"
    
    # Push tag to remote to trigger CI/CD
    Write-Info "Pushing tag to remote (this will trigger CI/CD pipeline)..."
    git push origin $tagName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to push tag to remote"
        Write-Warning "You may need to push manually: git push origin $tagName"
        exit 1
    }
    Write-Success "Tag pushed to remote - CI/CD pipeline will start automatically"
    Write-Info "View pipeline at: https://github.com/panoramicdata/SchemaMagic/actions"
}

# Publish to NuGet if requested
if ($PublishToNuGet) {
    Write-Header "Publishing to NuGet"
    
    # Get API key
    if (-not $ApiKey) {
        $ApiKey = $env:NUGET_API_KEY
    }
    
    if (-not $ApiKey) {
        Write-Error "NuGet API key not provided. Use -ApiKey parameter or set NUGET_API_KEY environment variable."
        exit 1
    }
    
    $nupkgFile = Get-ChildItem "SchemaMagic/nupkg" -Filter "SchemaMagic.$fullVersion.nupkg" | Select-Object -First 1
    
    if (-not $nupkgFile) {
        Write-Error "Package file not found: SchemaMagic.$fullVersion.nupkg"
        exit 1
    }
    
    if ($DryRun) {
        Write-Warning "[DRY RUN] Would publish package: $($nupkgFile.Name)"
    } else {
        Write-Info "Publishing $($nupkgFile.Name) to NuGet.org..."
        
        dotnet nuget push $nupkgFile.FullName `
            --api-key $ApiKey `
            --source https://api.nuget.org/v3/index.json `
            --skip-duplicate
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to publish package to NuGet"
            exit 1
        }
        
        Write-Success "Package published to NuGet.org"
        Write-Info "View at: https://www.nuget.org/packages/SchemaMagic/$version"
    }
} else {
    Write-Info "Skipping NuGet publish (use -PublishToNuGet to publish)"
    Write-Warning "Note: CI/CD pipeline will handle NuGet publishing automatically"
}

# Summary
Write-Header "Summary"
Write-Success "Version: $fullVersion"
Write-Success "Tag: $tagName"
Write-Success "Package: SchemaMagic/nupkg/SchemaMagic.$fullVersion.nupkg"

if ($DryRun) {
    Write-Warning "DRY RUN - No changes were made"
} else {
    Write-Success "Tag pushed to remote - CI/CD will now:"
    Write-Info "  1. Run all tests"
    Write-Info "  2. Build and publish to NuGet"
    Write-Info "  3. Create GitHub Release"
    Write-Info "  4. Deploy web application"
    Write-Info ""
    Write-Info "Monitor progress at: https://github.com/panoramicdata/SchemaMagic/actions"
    
    if ($PublishToNuGet) {
        Write-Warning "You used -PublishToNuGet flag, but CI/CD will also publish."
        Write-Warning "This may result in duplicate publish attempts (harmless with --skip-duplicate)"
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "?? Publish process complete!" -ForegroundColor Green
