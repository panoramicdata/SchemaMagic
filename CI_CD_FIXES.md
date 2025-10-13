# CI/CD Build Fixes

## Issues Fixed

### 1. ? JavaScript Duplicate Declaration Error
**Error**: `Uncaught SyntaxError: redeclaration of const STORAGE_KEYS`

**Root Cause**: The `STORAGE_KEYS` constant was declared in both `variables.js` and `settings.js`, causing a redeclaration error when the JavaScript files were combined in the HTML template.

**Fix**: Removed the duplicate `STORAGE_KEYS` declaration from `Templates/settings.js` since it's already defined in `Templates/variables.js`.

**Files Changed**:
- `Templates/settings.js` - Removed duplicate STORAGE_KEYS declaration

### 2. ? Modal Dialogs Opening on Page Load
**Error**: Table Grouping Rules dialog and Icon Picker dialog were visible on page load instead of being hidden until user interaction.

**Root Cause**: The modal overlay and dialog elements in `Templates/template.html` were missing the `style="display: none;"` attribute.

**Fix**: Added `style="display: none;"` to all modal dialog elements to ensure they're hidden by default.

**Files Changed**:
- `Templates/template.html` - Added display:none to modal overlays and dialogs

### 3. ? CI/CD Build Failure - Case-Sensitive Paths
**Error**: 
```
CSC : error CS1566: Error reading resource 'SchemaMagic.Core.Templates.template.html' -- 
'Could not find a part of the path '/home/runner/work/SchemaMagic/SchemaMagic/Templates/template.html'.'
```

**Root Cause**: The embedded resource paths in `SchemaMagic.Core/SchemaMagic.Core.csproj` were using lowercase `templates` instead of capitalized `Templates`. Linux/Unix build servers are case-sensitive, so the paths didn't match the actual folder name.

**Fix**: Updated all embedded resource paths from `..\templates\` to `..\Templates\` to match the actual folder name (case-sensitive).

**Files Changed**:
- `SchemaMagic.Core/SchemaMagic.Core.csproj` - Fixed case-sensitive paths for all embedded resources

### 4. ? Missing Embedded Resource
**Issue**: The new `table-grouping.js` file was not included in the embedded resources list.

**Fix**: Added `table-grouping.js` to the embedded resources in the csproj file.

**Files Changed**:
- `SchemaMagic.Core/SchemaMagic.Core.csproj` - Added table-grouping.js to embedded resources

## Summary of Changes

### Files Modified:
1. `Templates/settings.js` - Removed duplicate STORAGE_KEYS declaration
2. `Templates/template.html` - Added display:none to modal dialogs  
3. `SchemaMagic.Core/SchemaMagic.Core.csproj` - Fixed case-sensitive paths and added missing resource

### Build Status:
- ? Local build: **Successful**
- ?? CI/CD build: **Ready for testing** (push changes to trigger)

## Next Steps

1. Commit and push these changes to trigger CI/CD
2. Verify GitHub Actions build succeeds
3. Verify web deployment to GitHub Pages works
4. Test the generated HTML schema files

## Testing Commands

```bash
# Local build verification
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Check Git status
git status

# Commit changes
git add .
git commit -m "Fix CI/CD build errors and JavaScript issues"

# Push to trigger CI/CD
git push origin main
```

## Root Cause Analysis

The CI/CD failures were caused by:

1. **Platform differences**: Windows is case-insensitive for file paths, but Linux (GitHub Actions) is case-sensitive
2. **Code organization**: Multiple JavaScript files being combined without proper duplicate checking
3. **Missing initialization**: Modal dialogs not properly initialized as hidden

These issues only manifested in CI/CD because:
- Local Windows development worked fine with case-insensitive paths
- Local browser testing didn't catch the duplicate declaration due to browser error recovery
- Manual testing didn't trigger the modal dialog auto-open issue

## Prevention Measures

To prevent similar issues:

1. **Use consistent casing**: Always use PascalCase for folder names (`Templates` not `templates`)
2. **Test on Linux**: Use WSL or Docker to test builds on Linux before pushing
3. **Code reviews**: Check for duplicate declarations when combining JavaScript files
4. **Automated tests**: Add tests that verify HTML generation and JavaScript validity
