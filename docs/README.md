# SchemaMagic Documentation

Welcome to the SchemaMagic documentation! This tool transforms your Entity Framework Core DbContexts into beautiful, interactive schema diagrams.

## Table of Contents

- [Getting Started](#getting-started)
- [Command Line Reference](#command-line-reference)
- [Interactive Features](#interactive-features)
- [Customization Guide](#customization-guide)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## Getting Started

### Installation

SchemaMagic is distributed as a .NET Global Tool. Install it with:

```bash
dotnet tool install -g SchemaMagic
```

### Your First Schema

Generate a schema from any Entity Framework Core DbContext:

```bash
schemamagic path/to/MyDbContext.cs
```

This creates an HTML file with an interactive schema diagram that you can open in any modern web browser.

## Command Line Reference

### Basic Usage
```bash
schemamagic <dbcontext-file> [options]
```

### Options

| Option | Description | Example |
|--------|-------------|---------|
| `--output <file>` | Specify output HTML file path | `--output my-schema.html` |
| `--css-file <file>` | Use custom CSS styling | `--css-file custom.css` |
| `--guid <guid>` | Preserve layout with specific GUID | `--guid "123e4567-e89b-12d3"` |
| `--output-default-css` | Export default CSS for customization | `--output-default-css styles.css` |
| `--version` | Show version information | |
| `--help` | Show help information | |

### Examples

```bash
# Basic generation
schemamagic Models/BlogContext.cs

# Custom output name
schemamagic Models/BlogContext.cs --output blog-diagram.html

# With custom styling
schemamagic Models/BlogContext.cs --css-file my-styles.css

# Export default CSS for customization
schemamagic --output-default-css default-styles.css
```

## Interactive Features

Once you open the generated HTML file, you can:

### Navigation
- **Drag Tables**: Click and drag any table to reposition it
- **Pan Canvas**: Drag the background to pan around large schemas
- **Zoom**: Use mouse wheel to zoom in and out
- **Navigate Relationships**: Click foreign key or navigation properties to jump to related entities

### Table Selection
- **Select Table**: Click any table to select it and highlight its relationships
- **Deselect Table**: Click the selected table again to deselect it
- **Keyboard Shortcut**: Press `Escape` to deselect any selected table

### Display Options
Use the toolbar buttons to toggle:
- **Relationships**: Show/hide relationship lines
- **Navigation Properties**: Show/hide navigation properties
- **Inherited Properties**: Show/hide inherited properties from base classes
- **Full Height Mode**: Show all properties vs. truncated view
- **Snap to Grid**: Enable/disable grid-based positioning

### Persistent Customization
- Your table positions and display preferences are automatically saved
- Settings persist across browser sessions using localStorage
- Each schema has a unique identifier for isolated customization

## Customization Guide

### Custom CSS Styling

1. Export the default CSS:
   ```bash
   schemamagic --output-default-css my-styles.css
   ```

2. Modify the CSS file to customize colors, fonts, sizes, etc.

3. Generate schema with custom styles:
   ```bash
   schemamagic MyDbContext.cs --css-file my-styles.css
   ```

### Layout Persistence

Each generated schema has a unique GUID for localStorage. To maintain the same layout when regenerating:

```bash
schemamagic MyDbContext.cs --guid "your-existing-guid"
```

You can find the GUID in the browser console when you open a schema.

### Property Icons and Colors

The tool automatically assigns icons and colors based on property characteristics:

- **?? PK**: Primary Key properties (yellow circle)
- **?? FK**: Foreign Key properties (purple circle)
- **?? N**: Navigation properties (green circle)
- **?? INH**: Inherited properties (blue circle)

Type-specific colors are used for property types:
- **Blue**: String types
- **Purple**: Numeric types (int, decimal, etc.)
- **Red**: GUID types
- **Gray**: DateTime and complex types
- **Green**: Navigation properties

## Understanding the Output

### Schema Elements

- **Tables**: Entity classes are rendered as tables with headers and property lists
- **Relationships**: Lines connect related entities with crow's foot notation
- **Inheritance**: Base classes and derived classes are visually connected
- **Properties**: Each property shows name, type, and appropriate icons

### Relationship Types

- **One-to-Many**: Single line to crow's foot
- **Many-to-Many**: Crow's foot to crow's foot (via junction tables)
- **One-to-One**: Single line to single line
- **Self-Referencing**: Curved lines back to the same table

### Physics-Based Layout

The initial layout uses a physics-based algorithm that:
- Minimizes line crossings
- Groups related entities
- Spreads tables for optimal readability
- Respects table sizes and content

## Troubleshooting

### Common Issues

**Q: The tool can't find my DbContext**
A: Ensure the file path is correct and the file contains a valid DbContext class.

**Q: Generated HTML file is blank**
A: Check the browser console for JavaScript errors. Ensure you're using a modern browser.

**Q: Tables overlap or look messy**
A: Try the physics-based layout by refreshing the page, or manually drag tables to better positions.

**Q: Relationships don't appear**
A: Ensure your entities have proper foreign key properties and navigation properties configured.

**Q: Custom CSS doesn't work**
A: Verify the CSS file path is correct and the file contains valid CSS.

### Browser Compatibility

SchemaMagic requires a modern web browser with:
- SVG support
- ES6+ JavaScript
- CSS Grid and Flexbox
- Local Storage

Supported browsers:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

### Performance

For large schemas (100+ entities):
- Use full-screen mode for better visibility
- Consider breaking large DbContexts into smaller, focused contexts
- Use the table selection feature to focus on specific areas
- Disable relationship lines temporarily for better performance

## Advanced Usage

### Entity Framework Patterns

SchemaMagic works best with DbContexts that follow standard EF Core patterns:

```csharp
public class MyDbContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        modelBuilder.Entity<Post>()
            .HasOne(p => p.Blog)
            .WithMany(b => b.Posts)
            .HasForeignKey(p => p.BlogId);
    }
}
```

### Inheritance Hierarchies

The tool automatically detects and visualizes:
- Table-per-hierarchy (TPH)
- Table-per-type (TPT)  
- Table-per-concrete-type (TPC)

### Data Annotations

SchemaMagic recognizes common data annotations:
- `[Key]` - Primary key properties
- `[ForeignKey]` - Foreign key relationships
- `[Required]` - Required properties
- `[MaxLength]` - String length constraints

## Contributing

We welcome contributions! See the main [README.md](../README.md) for contribution guidelines.

### Development Setup

1. Clone the repository
2. Install .NET 9.0 SDK
3. Run `dotnet build` to build the project
4. Run `dotnet run -- samples/BlogDbContext.cs` to test

### Template System

SchemaMagic uses a modular template system. Templates are in the `templates/` folder:
- `template.html` - Main HTML structure
- `styles.css` - All CSS styling
- `*.js` files - JavaScript modules for different features

To add new features, create new JavaScript files and they'll be automatically included.

For more technical details, see the source code documentation.