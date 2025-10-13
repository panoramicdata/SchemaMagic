# Quick Publishing Guide

## TL;DR - Publish to NuGet

```bash
# 1. Update version in SchemaMagic/SchemaMagic.csproj
<Version>1.1.0</Version>

# 2. Update CHANGELOG.md (move Unreleased to new version)

# 3. Build the package
cd SchemaMagic
dotnet pack -c Release -o ./nupkg

# 4. Test locally (optional but recommended)
dotnet tool install -g SchemaMagic --add-source ./nupkg
schemamagic --version
dotnet tool uninstall -g SchemaMagic

# 5. Publish to NuGet
dotnet nuget push nupkg/SchemaMagic.1.1.0.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json

# 6. Create Git tag
git tag -a v1.1.0 -m "Release version 1.1.0 - Property comment tooltips"
git push origin v1.1.0
```

## User Installation

Once published, users install with:

```bash
dotnet tool install -g SchemaMagic
```

Then use it:

```bash
schemamagic path/to/MyDbContext.cs
# or
dotnet schemamagic path/to/MyDbContext.cs
```

## Get Your NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key with "Push" permissions
3. Set expiration and package scopes as needed
4. Copy the key (you won't see it again!)

## Version Guidelines

- **Major (2.0.0)**: Breaking changes
- **Minor (1.1.0)**: New features, backward compatible
- **Patch (1.0.1)**: Bug fixes

Current release: Adding property tooltips = **Minor version bump** (1.0.0 ? 1.1.0)
