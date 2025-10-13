# Publishing SchemaMagic to NuGet

This guide explains how to publish SchemaMagic as a .NET Global Tool to NuGet.org.

## Prerequisites

1. **NuGet Account**: Create an account at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key from your [NuGet account settings](https://www.nuget.org/account/apikeys)
3. **.NET 9 SDK**: Ensure you have the latest .NET 9 SDK installed
4. **Nerdbank.GitVersioning CLI**: Automatically installed by Publish.ps1 if not present

## Automated Version Management

SchemaMagic uses **Nerdbank.GitVersioning (nbgv)** for automatic version management:

- **Major.Minor** (x.y): Defined in `version.json` (e.g., "1.0")
- **Patch** (z): Automatically calculated as Git commit height
- **Full Version**: `{Major}.{Minor}.{GitHeight}` (e.g., 1.0.42)

### version.json Configuration

```json
{
  "version": "1.0",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/tags/v\\d+\\.\\d+"
  ]
}
```

## Quick Start: Automated Publishing

### Option 1: Build and Tag Only (Recommended for Testing)

```powershell
.\Publish.ps1
```

This will:
- ? Calculate version from git history (e.g., 1.0.42)
- ? Build the NuGet package
- ? Create git tag (e.g., v1.0.42)
- ? Display package location for manual testing

### Option 2: Full Publish to NuGet

```powershell
.\Publish.ps1 -PublishToNuGet -ApiKey "YOUR_NUGET_API_KEY"
```

Or using environment variable:

```powershell
$env:NUGET_API_KEY = "YOUR_NUGET_API_KEY"
.\Publish.ps1 -PublishToNuGet
```

### Option 3: Dry Run (Test Without Changes)

```powershell
.\Publish.ps1 -DryRun -PublishToNuGet
```

## Version Bumping

### Patch Version (Automatic)

Just commit changes and run Publish.ps1:

```powershell
git add .
git commit -m "Add new feature"
.\Publish.ps1
# Version automatically increments: 1.0.41 ? 1.0.42
```

### Minor Version Bump

Update `version.json`:

```json
{
  "version": "1.1"
}
```

Then commit and publish:

```powershell
git add version.json
git commit -m "Bump minor version to 1.1"
.\Publish.ps1 -PublishToNuGet
# New version: 1.1.0
```

### Major Version Bump

Update `version.json`:

```json
{
  "version": "2.0"
}
```

Then commit and publish:

```powershell
git add version.json
git commit -m "Bump major version to 2.0 - Breaking changes"
.\Publish.ps1 -PublishToNuGet
# New version: 2.0.0
```

## Package Configuration

The project is configured in `SchemaMagic/SchemaMagic.csproj`:

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>schemamagic</ToolCommandName>
<PackageId>SchemaMagic</PackageId>
<PackageProjectUrl>https://github.com/panoramicdata/SchemaMagic</PackageProjectUrl>
<RepositoryUrl>https://github.com/panoramicdata/SchemaMagic</RepositoryUrl>
```

Version is automatically managed by Nerdbank.GitVersioning.

## Manual Publishing (Not Recommended)

If you need to publish manually without Publish.ps1:

### 1. Build the Package

```bash
cd SchemaMagic
dotnet pack -c Release -o ./nupkg
```

### 2. Publish to NuGet

```bash
dotnet nuget push nupkg/SchemaMagic.*.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### 3. Create Git Tag Manually

```bash
# Get current version from nbgv
nbgv get-version

# Create and push tag
git tag -a v1.0.42 -m "Release version 1.0.42"
git push origin v1.0.42
```

## Testing the Package Locally

Before publishing to NuGet, test locally:

```bash
# Install from local package
dotnet tool install -g SchemaMagic --add-source ./SchemaMagic/nupkg

# Test the command
schemamagic --version
schemamagic --help

# Test GitHub integration
schemamagic --github-repo https://github.com/panoramicdata/SchemaMagic

# Uninstall after testing
dotnet tool uninstall -g SchemaMagic
```

## Installation by Users

Once published, users can install SchemaMagic with:

```bash
# Install globally
dotnet tool install -g SchemaMagic

# Use the tool - local file
schemamagic path/to/MyDbContext.cs

# Use the tool - GitHub repository
schemamagic --github-repo https://github.com/owner/repo

# Use with Personal Access Token for private repos
schemamagic --github-repo https://github.com/owner/privaterepo --github-token ghp_xxxxx
```

### Update Existing Installation

```bash
dotnet tool update -g SchemaMagic
```

### Uninstall

```bash
dotnet tool uninstall -g SchemaMagic
```

## Verification After Publishing

1. **Check NuGet.org**: Visit https://www.nuget.org/packages/SchemaMagic
2. **Wait for Indexing**: New versions may take 5-10 minutes to appear in search
3. **Test Installation**: 
   ```bash
   dotnet tool install -g SchemaMagic --version 1.0.42
   schemamagic --version
   ```

## GitHub Actions (CI/CD)

### Adding NuGet API Key to GitHub Secrets

1. Go to repository **Settings ? Secrets and variables ? Actions**
2. Add a new secret named `NUGET_API_KEY`
3. Paste your NuGet API key as the value

### Automated Publishing on Tag Push

The GitHub Actions workflow can be configured to automatically publish when a tag is pushed:

```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Required for nbgv
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build and Pack
        run: dotnet pack SchemaMagic/SchemaMagic.csproj -c Release -o ./nupkg
      
      - name: Publish to NuGet
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
```

## Troubleshooting

### Common Issues

**? Package already exists with this version**
- Nerdbank.GitVersioning prevents this by auto-incrementing with each commit
- If needed, make a new commit to increment the version

**? Invalid API key**
- Regenerate at https://www.nuget.org/account/apikeys
- Ensure the key has "Push" permissions
- Verify no extra spaces when copying the key

**? nbgv: command not found**
- Run: `dotnet tool install -g nbgv`
- Or let Publish.ps1 install it automatically

**? Uncommitted changes detected**
- Commit or stash your changes: `git add . && git commit -m "message"`
- Or use `-DryRun` to test without committing

**? Tag already exists**
- The version has already been published
- Make a new commit to increment the patch version
- Or update version.json for minor/major bump

**? Command not found after installation**
- Add `~/.dotnet/tools` to PATH (Linux/macOS)
- Add `%USERPROFILE%\.dotnet\tools` to PATH (Windows)
- Restart terminal after installation

## Version Examples

Starting from version.json: `"version": "1.0"`

| Commits | Git Height | Full Version | Tag |
|---------|-----------|--------------|-----|
| 0 | 0 | 1.0.0 | v1.0.0 |
| 5 commits | 5 | 1.0.5 | v1.0.5 |
| 10 commits | 10 | 1.0.10 | v1.0.10 |

After updating to `"version": "1.1"`:

| Commits | Git Height | Full Version | Tag |
|---------|-----------|--------------|-----|
| 0 (from version change) | 0 | 1.1.0 | v1.1.0 |
| 3 commits | 3 | 1.1.3 | v1.1.3 |

## Best Practices

1. ? **Let nbgv manage versions**: Don't manually edit version numbers in .csproj
2. ? **Test locally first**: Use `.\Publish.ps1` without `-PublishToNuGet`
3. ? **Use meaningful commits**: Each commit increments the patch version
4. ? **Minor version for features**: Update version.json for new features
5. ? **Major version for breaking changes**: Update version.json for breaking changes
6. ? **Keep version.json in main branch**: Don't use feature branches for version bumps
7. ? **Document breaking changes**: Update CHANGELOG.md before major version bumps

## Useful Commands

```bash
# Check current version (without building)
nbgv get-version

# Check current version (detailed)
nbgv get-version -f json

# List all tool versions
dotnet tool list -g

# Check if tool is installed
dotnet tool list -g | grep SchemaMagic

# Install specific version
dotnet tool install -g SchemaMagic --version 1.0.42

# Search for package on NuGet
dotnet tool search SchemaMagic

# View package statistics
https://www.nuget.org/stats/packages/SchemaMagic
```

## Support

For issues or questions:
- **GitHub Issues**: https://github.com/panoramicdata/SchemaMagic/issues
- **GitHub Discussions**: https://github.com/panoramicdata/SchemaMagic/discussions
- **Documentation**: https://github.com/panoramicdata/SchemaMagic/blob/main/README.md
