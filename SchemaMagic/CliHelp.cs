namespace SchemaMagic;

internal static class CliHelp
{
	public const string Description = @"
🎯 SchemaMagic - Interactive EF Core Schema Visualizer
=======================================================

Generate beautiful, interactive HTML schema diagrams from:
  • Local DbContext files
  • GitHub repositories (public or private)

Examples:
  Local file:
    schemamagic MyDbContext.cs
    schemamagic MyDbContext.cs --output my-schema.html
    schemamagic MyDbContext.cs --css-file custom-styles.css

  GitHub repository (public):
    schemamagic --github-repo https://github.com/panoramicdata/SchemaMagic

  GitHub repository (private with PAT):
    schemamagic --github-repo https://github.com/owner/privaterepo --github-token ghp_xxxxx

  Export default CSS:
    schemamagic --output-default-css
    schemamagic --output-default-css my-custom-styles.css

Features:
  ✨ Interactive drag-and-drop tables with auto-save
  🔍 Zoom, pan, and smart navigation
  🔗 Automatic relationship detection (FK, Nav Properties)
  🎨 Customizable styling with CSS support
  💾 Persistent layout (localStorage with deterministic GUIDs)
  🌙 Dark mode support
  🐙 GitHub integration (no cloning required)
  📦 Self-contained HTML (works offline)

GitHub Personal Access Token:
  For private repositories or higher rate limits, create a token at:
  https://github.com/settings/personal-access-tokens/new

  Required scope: 'repo' (full control) or 'public_repo' (public only)

More Information:
  📖 Documentation: https://github.com/panoramicdata/SchemaMagic
  🌐 Web App: https://panoramicdata.github.io/SchemaMagic
  📦 NuGet: https://www.nuget.org/packages/SchemaMagic
";

	private static readonly string[] _missingArgumentLines =
	[
		"❌ Error: Either specify a DbContext file or use --github-repo",
		"",
		"📖 Quick Start Examples:",
		"   Local:  schemamagic MyDbContext.cs",
		"   GitHub: schemamagic --github-repo https://github.com/owner/repo",
		"   CSS:    schemamagic --output-default-css",
		"",
		"💡 Use --help for full documentation"
	];

	public static void WriteMissingArgumentHelp()
	{
		foreach (var line in _missingArgumentLines)
		{
			Console.WriteLine(line);
		}
	}

	public static void WriteNoDbContextHelp()
	{
		Console.WriteLine("❌ No DbContext files found in the repository");
		Console.WriteLine();
		Console.WriteLine("💡 Troubleshooting:");
		Console.WriteLine("   • Ensure the repository contains Entity Framework Core DbContext classes");
		Console.WriteLine("   • Check that DbContext files have 'DbSet<>' properties");
		Console.WriteLine("   • If this is a private repo, provide --github-token");
	}
}
