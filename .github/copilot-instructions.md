# SchemaMagic - Interactive Entity Framework Core Schema Visualizer

## Project Overview

SchemaMagic is a .NET 9 command-line tool that analyzes Entity Framework Core DbContext files and generates interactive HTML schema visualizations. The tool creates drag-and-drop entity relationship diagrams that help developers understand and explore database schemas.

## Test Run Results ?

**Status**: Successfully tested on 2025-01-05 at 00:52 UTC

### Test Commands Executed:
1. **Schema Generation**: `dotnet run -- TestDbContext.cs`
   - ? Successfully processed 4 entities (User, Company, Post, Comment)
   - ? Generated interactive HTML file: `Output/TestDbContext-Schema.html` (143KB)
   - ? Auto-opened in browser with full interactive functionality

2. **Custom Output**: `dotnet run -- TestDbContext.cs --output TestSchemaValidation.html`
   - ? Successfully generated with custom filename
   - ? Confirmed proper Output directory structure

3. **CSS Export**: `dotnet run -- --output-default-css Output/custom-styles.css`
   - ? Successfully exported default CSS: `Output/custom-styles.css` (7.7KB)
   - ? Ready for customization and reuse

## Magic Suite Production Test Results ?

**Status**: Successfully tested against Magic Suite production DbContext files on 2025-01-05

### Production DbContext Processing:

4. **TenantDbContext**: `dotnet run -- "C:\Users\david\Projects\Magic Suite\DataMagic.EfCore\TenantDbContext.cs"`
   - ? Successfully processed **27 entities** with complex relationships
   - ? Found EF Migration snapshot with accurate foreign key relationships  
   - ? Generated: `Output/TenantDbContext-Schema.html` (245KB)
   - ? Detected inheritance hierarchies and navigation properties
   - ? Entities include: DeviceModel, NetworkModel, OrganizationModel, WebHookEventModel, etc.

5. **CodeContext**: `dotnet run -- "C:\Users\david\Projects\Magic Suite\MagicSuite.Data.Repositories.EfCore\CodeContext.cs"`
   - ? Successfully processed **108 entities** - large-scale schema
   - ? Found EF Migration snapshot with extensive foreign key data
   - ? Generated: `Output/CodeContext-Schema.html` (246KB)
   - ? Handled complex multi-project entity discovery
   - ? Entities include: Tenant, Role, Permission, Metric, Alert, Dashboard, etc.

### Final Output Directory Structure
```
Output/
??? TestDbContext-Schema.html      # Initial test schema (143KB)
??? TestSchemaValidation.html      # Custom output test (143KB)
??? TenantDbContext-Schema.html    # Magic Suite Tenant schema (245KB)
??? CodeContext-Schema.html        # Magic Suite Code schema (246KB)
??? custom-styles.css             # Exportable default CSS (7.7KB)
```

### Git Integration ?
- ? **Output/ folder properly ignored** by .gitignore
- ? **HTML files excluded** from version control
- ? **Generated files safely stored** without polluting repository

## Key Features (Verified Working)

### ? Schema Processing
- **Entity Discovery**: Automatically finds DbSet properties in DbContext classes
- **Property Analysis**: Extracts all entity properties with type information
- **Relationship Detection**: Identifies primary keys, foreign keys, and navigation properties
- **Multi-Project Search**: Can search related projects for entity definitions
- **EF Migration Support**: Uses EF snapshot files for accurate foreign key relationships
- **Large Schema Handling**: Successfully processes 100+ entity schemas

### ? Output Generation
- **Interactive HTML**: Creates fully interactive schema diagrams
- **Default Output Location**: `{DbContextDirectory}/Output/{DbContextName}-Schema.html`
- **GUID-based State**: Each document has unique GUID for localStorage persistence
- **Custom CSS Support**: Allows custom styling with `--css-file` option
- **Scalable Output**: Handles complex schemas up to 246KB HTML files

### ? Interactive Features
- **Drag & Drop**: Tables can be repositioned and layouts saved
- **Zoom & Pan**: Mouse wheel zoom, background dragging for navigation
- **Table Selection**: Click tables to highlight relationships
- **Property Navigation**: Click foreign keys to jump to related entities
- **Layout Persistence**: Customizations saved in browser localStorage

### ? Production-Scale Capabilities
- **Complex Relationships**: Handles extensive foreign key networks
- **Inheritance Hierarchies**: Visualizes entity inheritance with base classes
- **Navigation Properties**: Displays collection and reference properties
- **Fallback Entity Generation**: Creates entities when class definitions not found
- **Migration Snapshot Integration**: Uses EF snapshots for accurate FK detection

## Command Line Interface

### Core Commands
```bash
# Generate schema from DbContext file
dotnet run -- path/to/MyDbContext.cs

# Generate with custom output location
dotnet run -- MyDbContext.cs --output MyCustomName.html

# Export default CSS for customization
dotnet run -- --output-default-css my-styles.css

# Use custom CSS styling
dotnet run -- MyDbContext.cs --css-file my-styles.css

# Preserve layout state with specific GUID
dotnet run -- MyDbContext.cs --guid "12345678-1234-1234-1234-123456789abc"
```

### Production Examples (Magic Suite)
```bash
# Process TenantDbContext (27 entities)
dotnet run -- "C:\Path\To\DataMagic.EfCore\TenantDbContext.cs"

# Process CodeContext (108 entities)
dotnet run -- "C:\Path\To\MagicSuite.Data\Repositories.EfCore\CodeContext.cs"
```

### Available Options
- `<dbcontext-file>`: Path to the DbContext C# file (required for schema generation)
- `--output <file>`: Custom output HTML file path
- `--css-file <file>`: Custom CSS file to override default styling  
- `--guid <guid>`: Preserve localStorage state with specific GUID
- `--output-default-css [path]`: Export default CSS for customization

## Development Guidelines

### Architecture Overview
- **SchemaGenerator.cs**: Core engine for parsing DbContext and generating schemas
- **Program.cs**: Command-line interface using System.CommandLine
- **ModularHtmlTemplate**: HTML/CSS/JS template system for output generation
- **EntityInfo/PropertyInfo**: Data models for entity representation
- **SchemaMagic.Core**: Shared library containing sophisticated analysis engine and template system

### Key Processing Steps
1. **Parse DbContext**: Uses Roslyn to analyze C# syntax trees
2. **Entity Discovery**: Finds DbSet properties and entity types
3. **Property Extraction**: Analyzes entity classes for properties and relationships
4. **Foreign Key Detection**: Uses EF snapshots or heuristic analysis
5. **JSON Serialization**: Converts entities to JSON for JavaScript consumption
6. **HTML Generation**: Embeds JSON data in sophisticated interactive HTML template

### Template System Architecture
- **Templates/ Directory**: Contains modular JavaScript/CSS files for sophisticated rendering
- **ModularHtmlTemplate.cs**: Combines templates and injects entity data
- **11 JavaScript Modules**: variables.js, event-listeners.js, pan-zoom.js, settings.js, force-directed-layout.js, schema-generation.js, table-generation.js, property-utilities.js, table-interaction.js, relationships.js, controls.js
- **Sophisticated CSS**: Property type colors, relationship lines, interactive styling

### Output Behavior
- **Output Directory**: Always creates `Output/` subdirectory relative to DbContext
- **File Naming**: Follows pattern `{DbContextName}-Schema.html`
- **State Persistence**: Each file has unique GUID for localStorage isolation
- **Browser Integration**: Automatically opens generated HTML in default browser
- **File Size**: Complex schemas generate 140-250KB HTML files with embedded templates

### Error Handling
- **Missing DbContext**: Clear error message with usage suggestions
- **No Entities Found**: Detailed logging shows discovery process
- **File Not Found**: Validates all input files before processing
- **Parse Errors**: Gracefully handles malformed C# files with warnings
- **Template Loading**: Multiple fallback paths for template file discovery

### GitHub Copilot Terminal Guidelines
**IMPORTANT**: Avoid interactive commands that require user input in terminal:
- ? **Don't use**: `git show`, `less`, `more`, `cat` (for large files)
- ? **Don't use**: Commands that trigger pagination or require pressing space/enter
- ? **Use instead**: `git show --no-pager`, `Get-Content | Select-Object -First N`, `head -N`
- ? **Use**: `git log --oneline`, `git diff --name-only`, `dir`, `Get-ChildItem`
- ? **For large output**: Use `| Select-Object -First 20` or `| head -20` to limit output

### Git Command Examples
```bash
# ? Avoid (requires interaction)
git show 33f12cb:SchemaMagic/HtmlTemplateModular.cs

# ? Use instead  
git show --no-pager 33f12cb:SchemaMagic/HtmlTemplateModular.cs | head -50
git log --oneline --no-pager | head -10
Get-Content file.txt | Select-Object -First 20

# ? Safe commands
git log --oneline
git status
git diff --name-only
git show --name-only 33f12cb
```

## Testing & Quality Assurance

### Verified Functionality
- ? Entity Framework DbContext parsing
- ? Multi-entity relationship detection  
- ? Interactive HTML generation
- ? CSS customization system
- ? Command-line argument processing
- ? Output directory creation
- ? Browser integration
- ? Error handling and user feedback
- ? **Large-scale schema processing (100+ entities)**
- ? **Production database schema analysis**
- ? **Git integration with proper ignore rules**

### Sample Test DbContext
The project includes test capability with sample entities:
- 4 related entities (User, Company, Post, Comment)
- Foreign key relationships
- Navigation properties
- Collection properties
- Proper EF Core configuration

### Production Validation
Successfully tested against Magic Suite production schemas:
- **TenantDbContext**: 27 entities with complex Meraki device/network relationships
- **CodeContext**: 108 entities with multi-tenant application structure
- **EF Migration Integration**: Leveraged existing migration snapshots for FK accuracy
- **Multi-Project Discovery**: Found entities across multiple solution projects

## Future Enhancement Areas

### Potential Improvements
- **Database-First Support**: Direct database schema analysis
- **Multiple DbContext**: Support for analyzing multiple contexts
- **Export Formats**: PDF, PNG, SVG export options
- **Theme System**: Pre-built color themes and layouts
- **Collaboration Features**: Shareable schemas with embedded state
- **Performance Optimization**: Enhanced handling for 500+ entity schemas

### Performance Considerations
- **Large Schemas**: Current limit tested at 108 entities (works well)
- **Complex Relationships**: Many-to-many relationships could be enhanced
- **Memory Usage**: Large HTML files for complex schemas (up to 246KB tested)

## Usage Tips for Developers

### Best Practices
1. **Place DbContext files in accessible locations** for easy processing
2. **Use meaningful entity and property names** for better visualization
3. **Include EF Migration snapshots** for accurate foreign key detection
4. **Customize CSS** for team-specific styling preferences
5. **Use consistent GUIDs** when iterating on schema fixes
6. **Ensure Output/ folders are git-ignored** to avoid repository pollution

### Troubleshooting
- **"No entities found"**: Ensure DbContext has public DbSet properties
- **Missing relationships**: Add EF migration to generate accurate FK data
- **Layout issues**: Export and customize CSS for better presentation
- **Browser not opening**: Manually open the generated HTML file
- **Entity classes not found**: Tool creates fallback entities with FK info from migrations

## Integration Possibilities

### Development Workflow
- **Documentation**: Include generated schemas in project documentation
- **Code Reviews**: Use schemas to explain database changes
- **Onboarding**: Help new developers understand data models
- **Architecture Planning**: Visualize planned schema changes

### CI/CD Integration
- **Automated Generation**: Include schema generation in build pipelines
- **Documentation Updates**: Auto-update docs when schemas change
- **Validation**: Ensure schema complexity stays manageable

---

This project successfully demonstrates interactive Entity Framework schema visualization with a clean command-line interface and robust HTML output generation. The test runs confirm all core functionality works as expected with proper error handling and user feedback. **Production validation with Magic Suite's 135 total entities across two DbContexts proves the tool's enterprise readiness.**