# SchemaMagic Template System

This directory contains the modular template files for generating interactive database schema visualizations.

## File Structure

```
Templates/
??? template.html           # Main HTML structure with placeholders
??? styles.css             # All CSS styles and themes
??? variables.js           # Global variables and constants
??? event-listeners.js     # DOM event setup and initialization
??? pan-zoom.js           # Pan and zoom functionality
??? settings.js           # Local storage and settings management
??? schema-generation.js  # Schema generation and table positioning
??? table-generation.js  # Table rendering and SVG creation
??? property-utilities.js # Property grouping and navigation utilities
??? table-interaction.js # Table dragging and selection
??? relationships.js      # Relationship lines and connections
??? controls.js           # Toggle functions and UI controls
```

## Template System

The `ModularHtmlTemplate.cs` class combines these files at runtime:

1. **HTML Structure**: Loads `template.html` as the base
2. **CSS Injection**: Replaces `<!-- CSS STYLES -->` with `styles.css`
3. **JavaScript Assembly**: Combines all `.js` files in dependency order
4. **Entity Data**: Replaces `ENTITIES_JSON_PLACEHOLDER` with actual data

## Key Features

### ?? **Interactive Visualization**
- Drag tables to reposition them
- Pan and zoom the canvas 
- Click navigation properties to jump between tables
- Toggle various display options

### ?? **Smart Download Functionality**
The download button creates a complete snapshot of your current customization:

**What Gets Preserved:**
- ? **Zoom Level**: Current zoom percentage (1x to 15x)
- ? **Pan Position**: Exact view coordinates and canvas position
- ? **Table Positions**: Where you've dragged each table
- ? **Toggle Settings**: All toolbar button states (relationships, navigation, inherited, etc.)
- ? **Selected Table**: Currently selected/highlighted table
- ? **View Mode**: Full height mode, snap to grid settings

**How It Works:**
1. Captures current application state when download is clicked
2. Embeds a state restoration script into the HTML file
3. Downloads file with descriptive name: `schema-visualization-2024-01-15T14-30-45-zoom150%.html`
4. When opened, the file automatically restores your exact customization

**Technical Implementation:**
- State is injected as JavaScript that runs on page load
- Settings are stored in the downloaded file's localStorage
- Tables are repositioned to match your custom layout
- All toggles and view settings are restored

### ?? **Persistent State Management**
- Local storage automatically saves your preferences
- Settings persist across browser sessions
- Table positions are remembered between visits
- View preferences are maintained

## Benefits of Modular Design

### ? **Maintainability**
- Each file has a single responsibility
- Easy to locate and modify specific functionality
- Clear separation of concerns

### ? **Readability** 
- Smaller, focused files instead of one monolithic file
- Logical grouping of related functions
- Better code organization

### ? **Extensibility**
- Easy to add new features by creating new template files
- Simple to modify existing functionality without affecting other parts
- Plugin-like architecture for future enhancements

### ? **Debugging**
- Easier to identify issues in specific modules
- Better error isolation
- Simpler testing of individual components

## Adding New Features

1. **Create New Template File**: Add a new `.js` file in the Templates folder
2. **Update File List**: Add the filename to the `jsFiles` array in `ModularHtmlTemplate.cs`
3. **Embed Resource**: The `.csproj` already includes all `.js` files automatically

## File Dependencies

The JavaScript files are loaded in this order to ensure dependencies are met:

1. `variables.js` - Must be first (defines global state)
2. `event-listeners.js` - Sets up DOM event handlers
3. `pan-zoom.js` - Pan and zoom controls
4. `settings.js` - Settings persistence
5. `schema-generation.js` - Main schema generation logic
6. `table-generation.js` - SVG table rendering
7. `property-utilities.js` - Property processing utilities
8. `table-interaction.js` - Table dragging and selection
9. `relationships.js` - Relationship line rendering
10. `controls.js` - UI toggle functions **& smart download**

## User Experience Features

### ?? **Interactive Controls**
- **Drag & Drop**: Move tables anywhere on the canvas
- **Pan & Zoom**: Navigate large schemas with mouse controls
- **Smart Navigation**: Click underlined navigation properties to jump to related tables
- **Visual Feedback**: Hover effects, selection highlighting, boundary indicators

### ??? **Toolbar Controls**
- **Hide/Show Relations**: Toggle relationship lines between tables
- **Navigation Properties**: Show/hide navigation properties in tables  
- **Show/Hide Inherited**: Toggle inherited properties from base classes
- **Full Height**: Expand tables to show all properties
- **Snap to Grid**: Enable precise table alignment
- **Zoom Controls**: Zoom in, out, or reset to default
- **?? Smart Download**: Save your complete customization

### ?? **Visual Legend**
- **PK**: Primary Key (gold color)
- **FK**: Foreign Key (purple color)
- **NAV**: Navigation Property (green, clickable)
- **INH**: Inherited Property (blue, italic)

## Legacy Support

The original `HtmlTemplate.cs` is preserved for backward compatibility. New development should use the modular system via `ModularHtmlTemplate.cs`.

## Performance

- Templates are cached in memory after first load
- Embedded resources provide fast access
- File system fallback available during development
- State restoration runs efficiently on page load
- Download captures state without impacting performance