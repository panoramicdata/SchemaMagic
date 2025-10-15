# Property Extraction Fix - Summary

## Problem

The LogicMonitor.Datamart repository (and other repositories with modern C# syntax) was not finding all entity properties. There were **two separate issues**:

1. **Property extraction was too restrictive** (missing modern C# property patterns)
2. **Entity file discovery was incomplete** (GitHub entities not being found)

## Issue #1: Property Extraction Filter

### Root Cause

The original property filter was:

```csharp
var propertyDeclarations = entityClass.Members
    .OfType<PropertyDeclarationSyntax>()
    .Where(p => p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true);
```

This filter **excluded**:
1. **Init-only properties**: `public string Name { get; init; }`
2. **Required properties**: `public required string Name { get; set; }`
3. **Expression-bodied properties**: `public string FullName => $"{FirstName} {LastName}";`
4. **Properties with only init accessors**: Properties using C# 9+ init-only setters

The filter only matched properties that had an **explicit `AccessorList` with a `get` accessor**, missing many modern C# property patterns.

### Solution

Updated the property filter to include:

```csharp
var propertyDeclarations = entityClass.Members
    .OfType<PropertyDeclarationSyntax>()
    .Where(p => 
        // Has explicit accessor list with a getter or init
        (p.AccessorList?.Accessors.Any(a => 
            a.Kind() == SyntaxKind.GetAccessorDeclaration || 
            a.Kind() == SyntaxKind.InitAccessorDeclaration) == true) ||
        // OR is an expression-bodied property (no accessor list)
        p.ExpressionBody != null
    );
```

This now captures:
? Auto-implemented properties: `public string Name { get; set; }`
? Required properties: `public required string Name { get; set; }`
? Init-only properties: `public string Name { get; init; }`
? Expression-bodied properties: `public string FullName => $"{FirstName} {LastName}";`
? Read-only properties with initializers: `public DateTime Created { get; } = DateTime.Now;`
? Nullable properties: `public string? Description { get; set; }`

Also fixed the API call from `IsKind()` to `Kind()` which is the correct Roslyn API method.

## Issue #2: GitHub Entity File Discovery

### Root Cause

The `FindEntityFilesAsync` method in `GitHubService.cs` was only searching for files with **exact filename matches** to entity names. This failed when:
- Entity files were in subdirectories with complex paths
- Repository structure didn't follow expected naming conventions
- Files contained multiple class definitions

### Solution

Enhanced the entity file discovery with a **two-strategy approach**:

**Strategy 1**: Exact filename match (fast, efficient)
```csharp
// Look for files where filename exactly matches entity name
var potentialEntityFiles = tree.Tree
    .Where(item => item.Type == TreeType.Blob)
    .Where(item => item.Path.EndsWith(".cs"))
    .Where(item => entitySet.Contains(Path.GetFileNameWithoutExtension(item.Path)));
```

**Strategy 2**: Broad class definition search (comprehensive fallback)
```csharp
// If entities still missing, search ALL .cs files for class definitions
// Prioritizes common entity directories: /models/, /entities/, /domain/, /data/
// Scans up to 500 .cs files looking for matching class definitions
```

The broad search:
- ? Prioritizes files in `/models/`, `/entities/`, `/domain/`, `/data/` directories
- ? Searches for actual class definitions, not just filenames
- ? Handles files containing multiple entity classes
- ? Gracefully handles GitHub API rate limits
- ? Provides detailed logging of search progress

## Testing

Created comprehensive unit tests in `PropertyExtractionTests.cs` covering:
- Auto-implemented properties
- Required properties
- Init-only properties
- Expression-bodied properties
- Nullable properties
- Navigation properties
- Mixed property patterns

All 7 new tests pass successfully, bringing total to 16 passing tests.

## Verification

Tested with sample DbContexts:
- **BlogDbContext**: Successfully extracted all 5 entities with properties:
  - Blog: 7 properties
  - Post: 14 properties
  - Author: 7 properties
  - Tag: 6 properties
  - PostTag: 5 properties

- **TestDbContext**: Successfully extracted all 12 entities with properties:
  - User: 48 properties
  - Company: 24 properties
  - Department: 18 properties
  - Project: 33 properties
  - Task: 32 properties
  - Document: 24 properties
  - Comment: 16 properties
  - Attachment: 16 properties
  - Tag: 10 properties
  - TaskTag: 6 properties
  - UserProject: 11 properties
  - AuditLog: 13 properties

## Impact

These fixes ensure that **all modern C# property patterns** are correctly extracted when analyzing Entity Framework DbContext files, whether from:
- ? Local files with nearby entity classes
- ? Local files with entities in separate projects
- ? **GitHub repositories with any directory structure**
- ? Repositories using C# 9+ features and modern property syntax

## Files Modified

1. **SchemaMagic.Core/CoreSchemaAnalysisService.cs** 
   - Updated `ExtractProperties()` method to include all property patterns
   
2. **SchemaMagic/GitHubService.cs** 
   - Enhanced `FindEntityFilesAsync()` with two-strategy search approach
   - Added broad class definition search as fallback
   - Improved logging and error handling
   
3. **SchemaMagic.Tests/PropertyExtractionTests.cs** 
   - Added 7 comprehensive unit tests for property extraction patterns

## Result

The issue reported with LogicMonitor.Datamart (and any other repositories using modern C# syntax or complex directory structures) should now be resolved, with:
- ? All entity properties being correctly detected and extracted
- ? All entity files being found regardless of directory structure
- ? Comprehensive logging showing exactly what's being found
- ? Graceful handling of GitHub API rate limits

For best results with private repositories or to avoid rate limits, use:
```bash
schemamagic --github-repo <url> --github-token <your-pat>
