# SchemaMagic ✨

**Interactive HTML+SVG Database Schema Visualizer for Entity Framework Core**

Transform your Entity Framework DbContexts into beautiful, interactive schema diagrams with just one command.

[![NuGet Version](https://img.shields.io/nuget/v/SchemaMagic.svg)](https://www.nuget.org/packages/SchemaMagic)
[![Downloads](https://img.shields.io/nuget/dt/SchemaMagic.svg)](https://www.nuget.org/packages/SchemaMagic)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Web App](https://img.shields.io/badge/Web-App-blue.svg)](https://panoramicdata.github.io/SchemaMagic)

## 🌐 Try Online

**[🚀 SchemaMagic Web](https://panoramicdata.github.io/SchemaMagic)** - Analyze GitHub repositories online without installing anything!

## 🎯 Features

- 🖱️ **Interactive Tables**: Drag tables around, pan and zoom the canvas
- 🔗 **Smart Relationships**: Automatic detection of foreign keys and navigation properties
- 🎨 **Professional Styling**: Beautiful SVG rendering with hover effects and animations
- 🧲 **Physics-Based Layout**: Intelligent auto-positioning of tables for optimal readability
- 🔍 **Click to Navigate**: Click FK/Navigation properties to jump between related entities
- 📋 **Table Selection**: Click tables to highlight and filter their relationships
- 💾 **Persistent Customization**: Your layout changes are saved and restored automatically
- 📦 **Self-Contained**: Generated HTML files work offline with no external dependencies
- ⚡ **Fast Generation**: Parses C# code directly using Roslyn - no compilation needed

![SchemaMagic Demo](https://github.com/panoramicdata/SchemaMagic/raw/main/docs/demo.gif)

## 🚀 Quick Start

### Option 1: Web Application (No Installation)

Visit **[SchemaMagic Web](https://panoramicdata.github.io/SchemaMagic)** to:
- 📁 Analyze any public GitHub repository
- 🔍 Automatically discover DbContext files
- ⚡ Generate schemas instantly in your browser
- 💾 Download interactive HTML files

### Option 2: Command-Line Tool

Install as a global .NET tool:

```bash
dotnet tool install -g SchemaMagic
```

Generate an interactive schema from your DbContext:

```bash
# Basic generation
schemamagic path/to/MyDbContext.cs

# Custom output file
schemamagic MyDbContext.cs --output MyProjectSchema.html

# Include custom CSS styling
schemamagic MyDbContext.cs --css-file custom-styles.css
```

### Example Output

The generated HTML file creates an interactive diagram where you can:
- **Drag tables** to arrange them as you prefer
- **Click tables** to select and highlight their relationships 
- **Click again** to deselect and show all relationships
- **Navigate** by clicking foreign key and navigation properties
- **Zoom and pan** to explore large schemas
- **Toggle** inheritance and navigation property visibility

## 📖 Usage Guide

### Command Line Options

```bash
schemamagic [DbContext file path] [options]

Arguments:
  <dbcontext-file>    Path to the DbContext C# file

Options:
  --output <file>     Output HTML file path (default: auto-generated)
  --css-file <file>   Custom CSS file to override default styling  
  --guid <guid>       Preserve localStorage state with specific GUID
  --output-default-css Export default CSS for customization
  -h, --help          Show help information
  --version           Show version information
```

### Interactive Features

Once you open the generated HTML file:

- **🖱️ Drag Tables**: Click and drag any table to reposition it
- **🔍 Pan & Zoom**: Use mouse wheel to zoom, drag background to pan
- **🎯 Select Tables**: Click a table to select it and highlight its relationships
- **❌ Deselect Tables**: Click a selected table again to deselect it
- **🧭 Navigate**: Click foreign key or navigation properties to jump to related entities
- **⚙️ Toggle Options**: Use toolbar buttons to show/hide different elements
- **💾 Save Layout**: Your customizations are automatically saved in browser storage
- **⌨️ Keyboard**: Press `Escape` to deselect any selected table

### Relationship Visualization

SchemaMagic automatically detects and visualizes:

- **Foreign Key Relationships**: Shows connections between FK properties and primary keys
- **Navigation Properties**: Displays entity navigation properties with proper cardinality
- **Inheritance**: Visualizes entity inheritance hierarchies
- **Crow's Foot Notation**: Professional ERD-style relationship indicators

### Property Icons and Colors

- **🔑 PK**: Primary Key (yellow)
- **🔗 FK**: Foreign Key (purple) 
- **🧭 N**: Navigation Property (green)
- **📊 INH**: Inherited Property (blue)
- **Type Colors**: Different colors for strings, numbers, dates, etc.

## 🎨 Customization

### Custom Styling

Export the default CSS to customize appearance:

```bash
schemamagic --output-default-css my-custom.css
```

Then use your custom styles:

```bash
schemamagic MyDbContext.cs --css-file my-custom.css
```

### Layout Persistence

Each generated schema has a unique GUID for localStorage. To maintain layouts across regenerations:

```bash
schemamagic MyDbContext.cs --guid "your-custom-guid-here"
```

## 🛠️ Development

### Building from Source

```bash
git clone https://github.com/panoramicdata/SchemaMagic.git
cd SchemaMagic
dotnet build
dotnet run -- path/to/YourDbContext.cs
```

### Running the Web Application Locally

```bash
cd SchemaMagic.Web
dotnet run
```

The web application will be available at `https://localhost:5001`

### Running Tests

```bash
dotnet test
```

### Project Structure

```
SchemaMagic/
├── SchemaMagic/                # Main CLI tool project
├── SchemaMagic.Web/           # Blazor WebAssembly web application
├── templates/                  # Modular HTML/CSS/JS templates
├── samples/                    # Example DbContexts for testing
├── docs/                       # Documentation and examples
└── .github/workflows/          # CI/CD automation
```

## 📋 Requirements

- .NET 9.0 or later (for CLI tool)
- Entity Framework Core DbContext files (any version)
- Modern web browser for viewing generated HTML

## 🌐 Web Application Features

The **[SchemaMagic Web](https://panoramicdata.github.io/SchemaMagic)** application offers:

- 🔗 **GitHub Integration**: Connect to public repositories
- 🔍 **Auto-Discovery**: Find DbContext files automatically  
- ⚡ **Real-time Analysis**: Generate schemas without downloads
- 📱 **Mobile Friendly**: Works on all devices
- 💾 **Export Options**: Download interactive HTML files
- 🚀 **No Installation**: Works entirely in your browser

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Adding New Features

- **Template System**: Add new `.js` files to the `templates/` folder for client-side features
- **Schema Analysis**: Extend the Roslyn-based C# parsing in `SchemaGenerator.cs`
- **Styling**: Modify `templates/styles.css` for visual enhancements
- **Interactive Features**: Add JavaScript functions to appropriate template files
- **Web Application**: Enhance the Blazor WebAssembly app in `SchemaMagic.Web/`

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ❤️ by [Panoramic Data Limited](https://panoramicdata.com)
- Uses [Microsoft.CodeAnalysis](https://github.com/dotnet/roslyn) for C# parsing
- Uses [Octokit](https://github.com/octokit/octokit.net) for GitHub API integration
- Inspired by traditional ERD tools but designed for modern web browsers

## 🐛 Issues and Support

Found a bug or have a feature request? Please [open an issue](https://github.com/panoramicdata/SchemaMagic/issues) on GitHub.

For questions and discussions, visit our [GitHub Discussions](https://github.com/panoramicdata/SchemaMagic/discussions).
