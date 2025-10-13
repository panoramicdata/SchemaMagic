# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- ?? **Property Comment Tooltips**: Hover over property names to see comments from EF Core `[Comment]` attributes or XML documentation
  - Priority 1: EF Core `[Comment("...")]` attribute
  - Priority 2: XML documentation `/// <summary>` comments
  - Beautiful native browser tooltips with SVG `<title>` elements
  - Perfect for documenting database column purposes and business rules

## [1.0.0] - 2024-10-04

### Added
- ?? Interactive HTML+SVG database schema visualization for Entity Framework Core
- ??? Drag and drop table positioning with persistent layouts
- ?? Smart relationship detection for foreign keys and navigation properties
- ?? Professional styling with hover effects and animations
- ?? Physics-based auto-layout for optimal table positioning
- ?? Click-to-navigate functionality for entity relationships
- ??? Table selection and deselection with relationship filtering
- ?? Persistent customization using localStorage
- ? Fast C# parsing using Microsoft.CodeAnalysis (Roslyn)
- ?? Self-contained HTML output with no external dependencies
- ?? Customizable CSS styling system
- ?? Keyboard shortcuts (Escape to deselect)
- ?? Pan and zoom functionality
- ?? Inheritance visualization
- ?? Property icons (PK, FK, Navigation, Inherited)
- ?? Type-specific color coding
- ?? Crow's foot notation for relationships

### Technical Features
- Modular template system for easy extensibility
- Command-line interface with comprehensive options
- Support for all Entity Framework Core versions
- Cross-platform compatibility (.NET 9)
- Embedded resource template system
- GUID-based layout persistence
- Custom CSS override support

[Unreleased]: https://github.com/panoramicdata/SchemaMagic/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/panoramicdata/SchemaMagic/releases/tag/v1.0.0