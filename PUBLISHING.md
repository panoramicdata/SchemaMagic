# Publishing SchemaMagic to NuGet

This guide explains how to publish SchemaMagic as a .NET Global Tool to NuGet.org and deploy the web application to GitHub Pages.

## Prerequisites

1. **NuGet Account**: Create an account at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key from your [NuGet account settings](https://www.nuget.org/account/apikeys)
3. **.NET 9 SDK**: Ensure you have the latest .NET 9 SDK installed
4. **Nerdbank.GitVersioning CLI**: Automatically installed by Publish.ps1 if not present
5. **GitHub Repository**: With Pages enabled in repository settings

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

## Publishing Methods

### Method 1: Automated Publishing Script (Recommended)

The `Publish.ps1` script automates the entire release process by creating and pushing a Git tag, which triggers the CI/CD pipeline.

#### Quick Publish

```powershell
# 1. Commit all your changes
git add .
git commit -m "Your changes"
git push origin main

# 2. Run publish script (creates tag, pushes it, triggers CI/CD)
.\Publish.ps1
```

**What happens automatically:**
1. ? Version calculated from git history using nbgv (e.g., 1.0.42)
2. ? NuGet package built locally
3. ? Git tag created (e.g., v1.0.42)
4. ? **Tag pushed to GitHub (triggers CI/CD pipeline)**
5. ? CI/CD runs tests
6. ? CI/CD publishes to NuGet.org
7. ? CI/CD creates GitHub Release
8. ? CI/CD deploys web application to GitHub Pages

#### Dry Run (Test Without Changes)

```powershell
.\Publish.ps1 -DryRun
```

#### Manual NuGet Publish (Emergency Only)

```powershell
# This will ALSO trigger CI/CD (which will also publish)
# Only use if CI/CD is broken and you need immediate hotfix
.\Publish.ps1 -PublishToNuGet -ApiKey "YOUR_NUGET_API_KEY"
```

### Method 2: Manual Git Tag (Alternative)

You can also manually create and push tags to trigger CI/CD:

```bash
# Get current version
dotnet tool install -g nbgv
$version = nbgv get-version -v SimpleVersion

# Create and push tag
git tag -a "v$version" -m "Release version $version"
git push origin "v$version"
```

**What happens automatically:**
1. ? GitHub Actions CI/CD pipeline triggered
2. ? Tests run
3. ? NuGet package published
4. ? GitHub Release created
5. ? Web application deployed

## CI/CD Pipeline Configuration

### Trigger Rules

The CI/CD pipeline (`.github/workflows/ci-cd.yml`) **only** triggers on:

1. **Version tags** (`v*`) - e.g., `v1.0.42`, `v2.1.0`
2. **Manual workflow dispatch** (via GitHub Actions UI)

**Important:** The pipeline does **NOT** trigger on:
- ❌ Regular pushes to `main` or `develop`
- ❌ Pull requests
- ❌ Branch updates

This prevents unnecessary builds and ensures releases are intentional.

### Setup GitHub Secrets

For CI/CD to work, configure the NuGet API key:

1. Go to repository **Settings → Secrets and variables → Actions**
2. Add a new secret named `NUGET_API_KEY`
3. Paste your NuGet API key as the value

## Version Bumping

### Patch Version (Automatic)

Just commit changes and the patch version increments automatically:

```powershell
git add .
git commit -m "Fix bug in schema generation"
git push origin main

# If using manual publishing:
.\Publish.ps1 -PublishToNuGet

# If using GitHub Actions:
git tag -a v1.0.43 -m "Release version 1.0.43"
git push origin v1.0.43
```

Version automatically increments: `1.0.42 ? 1.0.43`

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
git commit -m "Bump minor version to 1.1 - Add new features"
git push origin main

# GitHub Actions method (recommended):
git tag -a v1.1.0 -m "Release version 1.1.0"
git push origin v1.1.0

# Manual method:
.\Publish.ps1 -PublishToNuGet
```

New version: `1.1.0`

### Major Version Bump (Breaking Changes)

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
git push origin main

# GitHub Actions method (recommended):
git tag -a v2.0.0 -m "Release version 2.0.0 - Breaking changes"
git push origin v2.0.0

# Manual method:
.\Publish.ps1 -PublishToNuGet
```

New version: `2.0.0`

## GitHub Pages Deployment

The web application is automatically deployed to GitHub Pages.

### Automatic Deployment Triggers

1. **When tags are pushed** (via `ci-cd.yml`)
2. **When main branch is updated** (via `ci-cd.yml`)
3. **When web files change** (via `deploy-web.yml`)
4. **Manual trigger** (via `deploy-web.yml` - Actions tab)

### Manual Deployment

If you need to manually deploy:

1. Go to **Actions** tab in GitHub
2. Select **"Deploy SchemaMagic Web to GitHub Pages"**
3. Click **"Run workflow"**
4. Select branch (usually `main`)
5. Click **"Run workflow"**

### Verify Deployment

After deployment:
- Visit: https://panoramicdata.github.io/SchemaMagic
- Check version: https://panoramicdata.github.io/SchemaMagic/version.txt
- Check deployment time: https://panoramicdata.github.io/SchemaMagic/deployed.txt

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

## Testing the Package Locally

Before publishing to NuGet, test locally:

```bash
# Install from local package
dotnet tool install -g SchemaMagic --add-source ./SchemaMagic/nupkg

# Test the command
schemamagic --version
schemamagic --help

# Test local file
schemamagic path/to/MyDbContext.cs

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

### NuGet Package

1. **Check NuGet.org**: Visit https://www.nuget.org/packages/SchemaMagic
2. **Wait for Indexing**: New versions may take 5-10 minutes to appear in search
3. **Test Installation**: 
   ```bash
   dotnet tool install -g SchemaMagic --version 1.0.42
   schemamagic --version
   ```

### Web Application

1. **Check Deployment**: Visit https://panoramicdata.github.io/SchemaMagic
2. **Verify Version**: Check version.txt file
3. **Test Functionality**: Try analyzing a repository

### GitHub Release

1. **Check Releases**: Visit https://github.com/panoramicdata/SchemaMagic/releases
2. **Verify Release Notes**: Ensure notes are correct
3. **Check Assets**: Download links should work

## Troubleshooting

### Common Issues

**? Package already exists with this version**
- Nerdbank.GitVersioning prevents this by auto-incrementing with each commit
- If needed, make a new commit to increment the version

**? Invalid API key**
- Regenerate at https://nuget.org/account/apikeys
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

**? GitHub Pages deployment failed**
- Check repository settings: Pages is enabled
- Source should be "GitHub Actions"
- Check workflow run in Actions tab

**? Web app shows 404 errors**
- Ensure `.nojekyll` file exists in deployment
- Check base href is set to `/SchemaMagic/`
- Verify 404.html is copied from index.html

**? GitHub Actions workflow not triggering**
- Ensure NUGET_API_KEY secret is set
- Check branch protection rules
- Verify tag format matches pattern (v*)

## Version Examples

Starting from version.json: `"version": "1.0"`

| Commits | Git Height | Full Version | Tag | Actions |
|---------|-----------|--------------|-----|---------|
| 0 | 0 | 1.0.0 | v1.0.0 | NuGet + GitHub Pages |
| 5 commits | 5 | 1.0.5 | v1.0.5 | NuGet + GitHub Pages |
| 10 commits | 10 | 1.0.10 | v1.0.10 | NuGet + GitHub Pages |

After updating to `"version": "1.1"`:

| Commits | Git Height | Full Version | Tag | Actions |
|---------|-----------|--------------|-----|---------|
| 0 (from version change) | 0 | 1.1.0 | v1.1.0 | NuGet + GitHub Pages |
| 3 commits | 3 | 1.1.3 | v1.1.3 | NuGet + GitHub Pages |

## Best Practices

1. ? **Use GitHub Actions for releases**: Consistent, automated process
2. ? **Let nbgv manage versions**: Don't manually edit version numbers in .csproj
3. ? **Test locally first**: Use `.\Publish.ps1` or `-DryRun` without publishing
4. ? **Use meaningful commits**: Each commit increments the patch version
5. ? **Minor version for features**: Update version.json for new features
6. ? **Major version for breaking changes**: Update version.json for breaking changes
7. ? **Keep version.json in main branch**: Don't use feature branches for version bumps
8. ? **Document breaking changes**: Update CHANGELOG.md before major version bumps
9. ? **Tag consistently**: Always use `v{Major}.{Minor}.{Patch}` format
10. ? **Monitor deployments**: Check Actions tab for deployment status

## Workflow Comparison

| Method | NuGet | GitHub Pages | Auto-Tag | Complexity |
|--------|-------|--------------|----------|------------|
| GitHub Actions | ? | ? | ? (manual) | Low |
| Publish.ps1 | ? | ? | ? | Medium |
| Manual | ? | ? | ? | High |

**Recommendation:** Use GitHub Actions for all production releases.

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
# https://www.nuget.org/stats/packages/SchemaMagic

# Check GitHub Pages status
curl -I https://panoramicdata.github.io/SchemaMagic

# View deployment version
curl https://panoramicdata.github.io/SchemaMagic/version.txt
```

## Support

For issues or questions:
- **GitHub Issues**: https://github.com/panoramicdata/SchemaMagic/issues
- **GitHub Discussions**: https://github.com/panoramicdata/SchemaMagic/discussions
- **Documentation**: https://github.com/panoramicdata/SchemaMagic/blob/main/README.md
- **Web Application**: https://panoramicdata.github.io/SchemaMagic
