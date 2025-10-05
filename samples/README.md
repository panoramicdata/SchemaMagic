# Sample DbContexts

This folder contains example Entity Framework Core DbContext files that demonstrate various SchemaMagic features and capabilities.

## Available Samples

### BlogDbContext.cs
A simple blog system demonstrating:
- Basic entity relationships (one-to-many)
- Many-to-many relationships through junction table
- Foreign key properties
- Navigation properties
- Simple data types

**Usage:**
```bash
schemamagic Samples/BlogDbContext.cs --output blog-schema.html
```

### ECommerceDbContext.cs
A more complex e-commerce system demonstrating:
- **Inheritance hierarchies** (Product ? PhysicalProduct/DigitalProduct, Address ? BillingAddress/ShippingAddress)
- **Self-referencing relationships** (Category parent/child)
- **Multiple foreign keys** to the same entity type
- **Abstract base classes** with audit fields
- **Enums** and complex data types
- **Data annotations** and validation attributes

**Usage:**
```bash
schemamagic Samples/ECommerceDbContext.cs --output ecommerce-schema.html
```

## Testing SchemaMagic Features

These samples allow you to test all major SchemaMagic features:

### Interactive Features
1. **Drag and Drop**: Move tables around to see layout persistence
2. **Table Selection**: Click tables to highlight their relationships
3. **Navigation**: Click foreign key and navigation properties to jump between entities
4. **Zoom and Pan**: Use mouse wheel and drag to explore large schemas

### Visualization Features
1. **Inheritance**: See how base classes and derived classes are displayed
2. **Relationships**: View one-to-many, many-to-many, and self-referencing relationships
3. **Property Icons**: See PK (Primary Key), FK (Foreign Key), N (Navigation), INH (Inherited) indicators
4. **Type Colors**: Different colors for strings, integers, dates, etc.

### Customization Features
1. **Toggle Inheritance**: Show/hide inherited properties
2. **Toggle Navigation**: Show/hide navigation properties
3. **Full Height Mode**: Show all properties vs. limited view
4. **Custom Styling**: Try the --css-file option with custom styles

## Creating Your Own Samples

To add your own sample DbContext:

1. Create a new `.cs` file in this folder
2. Define your DbContext class with entities
3. Use attributes and fluent configuration to define relationships
4. Test with: `schemamagic Samples/YourDbContext.cs`

### Tips for Good Sample DbContexts

- **Use descriptive names** for entities and properties
- **Include various relationship types** (1:1, 1:N, N:M)
- **Add inheritance hierarchies** to show advanced features  
- **Use data annotations** for additional metadata
- **Include navigation properties** for better visualization
- **Add comments** to explain complex relationships

## Expected Output

Each sample will generate a self-contained HTML file that you can open in any modern web browser. The generated files include:

- Interactive SVG schema diagram
- Drag-and-drop table positioning
- Relationship visualization with crow's foot notation
- Click-to-navigate functionality
- Zoom and pan controls
- Toggle buttons for various display options

## Troubleshooting

If you encounter issues with the samples:

1. **Check file paths**: Ensure you're running from the correct directory
2. **Verify .NET version**: Samples use .NET 9 features
3. **Check output**: Look for generated HTML files in the current directory
4. **Browser compatibility**: Use a modern browser (Chrome, Firefox, Edge, Safari)

For more information, see the main [README.md](../README.md) file.