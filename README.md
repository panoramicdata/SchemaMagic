# SchemaMagic ✨

**Interactive HTML+SVG Database Schema Visualizer for Entity Framework Core**

Transform your Entity Framework DbContexts into beautiful, interactive schema diagrams with just one command - from local files or GitHub repositories.

[![NuGet Version](https://img.shields.io/nuget/v/SchemaMagic.svg)](https://www.nuget.org/packages/SchemaMagic)
[![Downloads](https://img.shields.io/nuget/dt/SchemaMagic.svg)](https://www.nuget.org/packages/SchemaMagic)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Web App](https://img.shields.io/badge/Web-App-blue.svg)](https://panoramicdata.github.io/SchemaMagic)
[![GitHub](https://img.shields.io/github/stars/panoramicdata/SchemaMagic?style=social)](https://github.com/panoramicdata/SchemaMagic)

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
- 💬 **Property Tooltips**: Hover over properties to see comments from `[Comment]` attributes or XML documentation
- 🐙 **GitHub Integration**: Analyze repositories directly without cloning
- 🌙 **Dark Mode Support**: Automatic dark mode based on system preferences

## 🚀 Quick Start

### Option 1: Web Application (No Installation)

Visit **[SchemaMagic Web](https://panoramicdata.github.io/SchemaMagic)** to:
- 📁 Analyze any public GitHub repository
- 🔒 Support for private repositories with Personal Access Token
- 🔍 Automatically discover DbContext files
- ⚡ Generate schemas instantly in your browser
- 💾 Download interactive HTML files

### Option 2: Command-Line Tool

Install as a global .NET tool:

```bash
dotnet tool install -g SchemaMagic
```

Generate schemas from local files or GitHub repositories:

```bash
# Local DbContext file
schemamagic path/to/MyDbContext.cs

# GitHub repository (public)
schemamagic --github-repo https://github.com/owner/repo

# GitHub repository (private with PAT)
schemamagic --github-repo https://github.com/owner/repo --github-token ghp_xxxxx

# Custom output location
schemamagic MyDbContext.cs --output MyProjectSchema.html

# Apply custom CSS styling
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
- **Hide toolbar** (Ctrl+H) for distraction-free viewing

## 📖 Usage Guide

### Command Line Options

```bash
schemamagic [options] [<dbcontext-file>]

Arguments:
  <dbcontext-file>                Path to the DbContext C# file (local analysis)

Options:
  --github-repo <url>             GitHub repository URL (e.g., https://github.com/owner/repo)
  --github-token <token>          GitHub Personal Access Token for private repos
  --output <file>                 Output HTML file path (default: Output/{DbContext}-Schema.html)
  --css-file <file>               Custom CSS file to override default styling  
  --guid <guid>                   Custom document GUID to preserve localStorage state
  --output-default-css [path]     Export default CSS for customization
  -h, --help                      Show help and usage information
  --version                       Show version information

Examples:
  Local file:
    schemamagic MyDbContext.cs
    schemamagic MyDbContext.cs --output my-schema.html
    schemamagic MyDbContext.cs --css-file custom-styles.css

  GitHub repository:
    schemamagic --github-repo https://github.com/owner/repo
    schemamagic --github-repo https://github.com/owner/repo --github-token ghp_xxx
    
  Export default CSS:
    schemamagic --output-default-css
    schemamagic --output-default-css my-custom-styles.css
```

### GitHub Integration Features

**Public Repositories:**
```bash
schemamagic --github-repo https://github.com/panoramicdata/SchemaMagic
```

**Private Repositories:**
```bash
# Using command-line option
schemamagic --github-repo https://github.com/myorg/privaterepo --github-token ghp_xxxxx

# Using environment variable
$env:GITHUB_TOKEN = "ghp_xxxxx"
schemamagic --github-repo https://github.com/myorg/privaterepo --github-token $env:GITHUB_TOKEN
```

**How to create a GitHub Personal Access Token:**
1. Go to [GitHub Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens)
2. Click "Generate new token" → "Generate new token (classic)"
3. Give it a descriptive name (e.g., "SchemaMagic CLI")
4. Select scope: `repo` (for private repos) or `public_repo` (for public repos only)
5. Click "Generate token" and copy the token (starts with `ghp_`)

**Benefits of GitHub Integration:**
- ✅ No need to clone repositories
- ✅ Automatically discovers all DbContext files
- ✅ Generates deterministic GUIDs (preserves layout across runs)
- ✅ Processes multiple DbContext files in one command
- ✅ Works with both public and private repositories

### Interactive Features

Once you open the generated HTML file:

- **🖱️ Drag Tables**: Click and drag any table to reposition it
- **🔍 Pan & Zoom**: Use mouse wheel to zoom, drag background to pan
- **🎯 Select Tables**: Click a table to select it and highlight its relationships
- **❌ Deselect Tables**: Click a selected table again to deselect it
- **🧭 Navigate**: Click foreign key or navigation properties to jump to related entities
- **⚙️ Toggle Options**: Use toolbar buttons to show/hide different elements
- **💾 Save Layout**: Your customizations are automatically saved in browser storage
- **⌨️ Keyboard**: Press `Escape` to deselect, `Ctrl+H` to toggle toolbar
- **👆 Click Anywhere**: When toolbar is hidden, click anywhere to show it again

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

### Property Documentation Tooltips

SchemaMagic displays property comments as tooltips when you hover over property names:

**Priority 1: EF Core Comment Attribute**
```csharp
[Comment("Official company name")]
public string Name { get; set; }
```

**Priority 2: XML Documentation Comments**
```csharp
/// <summary>
/// User's given name
/// </summary>
public string FirstName { get; set; }
```

When you hover over properties with comments in the generated schema, you'll see beautiful native browser tooltips displaying the documentation. This is perfect for:
- 📝 Documenting column purposes and business rules
- 🔍 Providing context without cluttering the visualization  
- 📚 Sharing domain knowledge with your team
- ✅ Following documentation best practices

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

Each generated schema has a unique GUID for localStorage. When using GitHub integration, the GUID is deterministically generated from the repository URL and file path, ensuring your layout customizations persist across regenerations.

For local files, you can specify a custom GUID to maintain layouts:

```bash
schemamagic MyDbContext.cs --guid "your-custom-guid-here"
```

## 🛠️ Development

### Building from Source

```bash
git clone https://github.com/panoramicdata/SchemaMagic.git
cd SchemaMagic
dotnet build
dotnet run --project SchemaMagic -- path/to/YourDbContext.cs
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
├── SchemaMagic.Core/          # Core schema analysis library
├── SchemaMagic.Web/           # Blazor WebAssembly web application
├── Templates/                  # Modular HTML/CSS/JS templates
├── docs/                       # Documentation and examples
├── version.json               # Nerdbank.GitVersioning configuration
├── Publish.ps1                # Automated publishing script
└── .github/workflows/          # CI/CD automation
```

## 📋 Requirements

- **.NET 9.0 or later** (for CLI tool and development)
- **Entity Framework Core DbContext files** (any version)
- **Modern web browser** for viewing generated HTML
- **Git** (for version management during development)
- **PowerShell** (optional, for Publish.ps1 script)

## 🌐 Web Application Features

The **[SchemaMagic Web](https://panoramicdata.github.io/SchemaMagic)** application offers:

- 🔗 **GitHub Integration**: Connect to public and private repositories
- 🔍 **Auto-Discovery**: Find DbContext files automatically  
- ⚡ **Real-time Analysis**: Generate schemas without downloads
- 📱 **Mobile Friendly**: Works on all devices
- 💾 **Export Options**: Download interactive HTML files
- 🚀 **No Installation**: Works entirely in your browser
- 🌙 **Dark Mode**: Automatic theme based on system preferences
- 🔒 **Secure**: Personal Access Tokens processed client-side only

## 📦 Publishing & Versioning

SchemaMagic uses **Nerdbank.GitVersioning** for automatic version management:

- Version format: `{Major}.{Minor}.{GitHeight}` (e.g., 1.0.42)
- Major.Minor defined in `version.json`
- Patch number automatically calculated from git commit count

### For Maintainers

See [PUBLISHING.md](PUBLISHING.md) for detailed publishing instructions.

Quick publish:

```powershell
# Test build
.\Publish.ps1

# Publish to NuGet
.\Publish.ps1 -PublishToNuGet -ApiKey "YOUR_NUGET_API_KEY"

# Dry run (test without changes)
.\Publish.ps1 -DryRun -PublishToNuGet
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Adding New Features

- **Template System**: Add new `.js` files to the `Templates/` folder for client-side features
- **Schema Analysis**: Extend the Roslyn-based C# parsing in `CoreSchemaAnalysisService.cs`
- **Styling**: Modify `Templates/styles.css` for visual enhancements
- **Interactive Features**: Add JavaScript functions to appropriate template files
- **Web Application**: Enhance the Blazor WebAssembly app in `SchemaMagic.Web/`
- **GitHub Integration**: Extend `GitHubService.cs` for additional repository features

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ❤️ by [Panoramic Data Limited](https://panoramicdata.com)
- Uses [Microsoft.CodeAnalysis](https://github.com/dotnet/roslyn) for C# parsing
- Uses [Octokit](https://github.com/octokit/octokit.net) for GitHub API integration
- Uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) for version management
- Inspired by traditional ERD tools but designed for modern web browsers

## 🐛 Issues and Support

Found a bug or have a feature request? Please [open an issue](https://github.com/panoramicdata/SchemaMagic/issues) on GitHub.

For questions and discussions, visit our [GitHub Discussions](https://github.com/panoramicdata/SchemaMagic/discussions).

## 🔗 Links

- **NuGet Package**: https://www.nuget.org/packages/SchemaMagic
- **GitHub Repository**: https://github.com/panoramicdata/SchemaMagic
- **Web Application**: https://panoramicdata.github.io/SchemaMagic
- **Documentation**: https://github.com/panoramicdata/SchemaMagic/blob/main/README.md
- **Publishing Guide**: https://github.com/panoramicdata/SchemaMagic/blob/main/PUBLISHING.md
- **Changelog**: https://github.com/panoramicdata/SchemaMagic/blob/main/CHANGELOG.md
