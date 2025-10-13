# Publishing SchemaMagic to NuGet

This guide explains how to publish SchemaMagic as a .NET Global Tool to NuGet.org.

## Prerequisites

1. **NuGet Account**: Create an account at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key from your [NuGet account settings](https://www.nuget.org/account/apikeys)
3. **.NET 9 SDK**: Ensure you have the latest .NET 9 SDK installed

## Package Configuration

The project is already configured in `SchemaMagic/SchemaMagic.csproj`:

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>schemamagic</ToolCommandName>
<PackageId>SchemaMagic</PackageId>
<Version>1.0.0</Version>
```

This configuration:
- ? Packages as a .NET Global Tool
- ? Creates the `schemamagic` command
- ? Includes README and icon in the package

## Version Bumping

Before publishing, update the version number in:

1. **SchemaMagic/SchemaMagic.csproj**:
   ```xml
   <Version>1.1.0</Version>
   ```

2. **CHANGELOG.md**: Move Unreleased changes to a new version section

3. **Git Tag**: Create and push a version tag:
   ```bash
   git tag -a v1.1.0 -m "Release version 1.1.0"
   git push origin v1.1.0
   ```

## Building the Package

### Option 1: Manual Build

```bash
cd SchemaMagic
dotnet pack -c Release -o ./nupkg
```

This creates:
- `SchemaMagic.1.1.0.nupkg` - The package file
- `SchemaMagic.1.1.0.snupkg` - Symbol package (for debugging)

### Option 2: Using CI/CD

The GitHub Actions workflow `.github/workflows/ci-cd.yml` already handles building.

## Testing the Package Locally

Before publishing, test the package locally:

```bash
# Install from local package
dotnet tool install -g SchemaMagic --add-source ./SchemaMagic/nupkg

# Test the command
dotnet schemamagic --version

# Uninstall after testing
dotnet tool uninstall -g SchemaMagic
```

## Publishing to NuGet

### Option 1: Manual Publishing

```bash
cd SchemaMagic/nupkg

# Publish the package
dotnet nuget push SchemaMagic.1.1.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Publish symbols (optional but recommended)
dotnet nuget push SchemaMagic.1.1.0.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Option 2: Using GitHub Actions

1. **Add NuGet API Key to GitHub Secrets**:
   - Go to repository Settings ? Secrets and variables ? Actions
   - Add a new secret named `NUGET_API_KEY` with your API key value

2. **Update CI/CD Workflow** (`.github/workflows/ci-cd.yml`):
   ```yaml
   - name: Publish to NuGet
     if: startsWith(github.ref, 'refs/tags/v')
     run: |
       dotnet nuget push SchemaMagic/nupkg/*.nupkg \
         --api-key ${{ secrets.NUGET_API_KEY }} \
         --source https://api.nuget.org/v3/index.json \
         --skip-duplicate
   ```

3. **Trigger Publishing**:
   ```bash
   git tag -a v1.1.0 -m "Release version 1.1.0"
   git push origin v1.1.0
   ```

## Installation by Users

Once published, users can install SchemaMagic with:

```bash
# Install globally
dotnet tool install -g SchemaMagic

# Use the tool
dotnet schemamagic path/to/MyDbContext.cs

# Or just:
schemamagic path/to/MyDbContext.cs
```

### Update Existing Installation

```bash
dotnet tool update -g SchemaMagic
```

### Uninstall

```bash
dotnet tool uninstall -g SchemaMagic
```

## Verification

After publishing, verify the package:

1. **Check NuGet.org**: Visit https://www.nuget.org/packages/SchemaMagic
2. **Test Installation**: 
   ```bash
   dotnet tool install -g SchemaMagic --version 1.1.0
   schemamagic --version
   ```

## Package Statistics

View package statistics at:
- https://www.nuget.org/packages/SchemaMagic/
- https://www.nuget.org/stats/packages/SchemaMagic

## Troubleshooting

### Common Issues

**? Package already exists with this version**
- Increment the version number in `.csproj`
- NuGet does not allow overwriting published versions

**? Invalid API key**
- Regenerate your API key at https://www.nuget.org/account/apikeys
- Ensure the key has "Push" permissions

**? Package validation errors**
- Run `dotnet pack` and check for warnings
- Ensure README.md and icon.png exist

**? Command not found after installation**
- Ensure `~/.dotnet/tools` is in your PATH (Linux/macOS)
- Ensure `%USERPROFILE%\.dotnet\tools` is in your PATH (Windows)

## Best Practices

1. ? **Semantic Versioning**: Follow SemVer (MAJOR.MINOR.PATCH)
2. ? **Changelog**: Update CHANGELOG.md before each release
3. ? **Git Tags**: Tag releases for traceability
4. ? **Test Locally**: Always test the package before publishing
5. ? **Release Notes**: Include meaningful release notes in `.csproj`
6. ? **Documentation**: Keep README.md up to date
7. ? **Breaking Changes**: Increment MAJOR version for breaking changes

## Useful Commands

```bash
# Check current version
dotnet tool list -g | grep SchemaMagic

# Search for package on NuGet
dotnet tool search SchemaMagic

# Install specific version
dotnet tool install -g SchemaMagic --version 1.0.0

# Install from local source
dotnet tool install -g SchemaMagic --add-source ./nupkg

# Rollback to previous version
dotnet tool uninstall -g SchemaMagic
dotnet tool install -g SchemaMagic --version 1.0.0
```

## Support

For issues or questions:
- GitHub Issues: https://github.com/panoramicdata/SchemaMagic/issues
- GitHub Discussions: https://github.com/panoramicdata/SchemaMagic/discussions
