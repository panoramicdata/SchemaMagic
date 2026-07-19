using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using SchemaMagic.Core;

namespace SchemaMagic;

internal class Program
{
	static async Task<int> Main(string[] args)
	{
		var dbContextFileArgument = new Argument<FileInfo?>("dbcontext-file")
		{
			Description = "Path to the DbContext C# file (local file analysis)",
			Arity = ArgumentArity.ZeroOrOne
		};

		var githubRepoOption = new Option<string?>("--github-repo")
		{
			Description = "GitHub repository URL to analyze (e.g., https://github.com/panoramicdata/SchemaMagic)"
		};

		var githubTokenOption = new Option<string?>("--github-token")
		{
			Description = "GitHub Personal Access Token for private repositories or higher rate limits"
		};

		var outputOption = new Option<string?>("--output")
		{
			Description = "Output HTML file path (default: Output/{DbContext}-Schema.html)"
		};

		var guidOption = new Option<string?>("--guid")
		{
			Description = "Custom document GUID to preserve localStorage state (advanced)"
		};

		var cssFileOption = new Option<FileInfo?>("--css-file")
		{
			Description = "Path to custom CSS file for styling customization"
		};

		var outputDefaultCssOption = new Option<string?>("--output-default-css")
		{
			Description = "Export default CSS to file for customization (e.g., --output-default-css styles.css)"
		};

		var noOpenOption = new Option<bool>("--no-open")
		{
			Description = "Do not open the generated HTML in the default browser (useful for CI/automation)"
		};

		var rootCommand = new RootCommand("🎯 SchemaMagic - Interactive Entity Framework Core Schema Visualizer")
		{
			dbContextFileArgument,
			githubRepoOption,
			githubTokenOption,
			outputOption,
			guidOption,
			cssFileOption,
			outputDefaultCssOption,
			noOpenOption
		};

		rootCommand.Description = @"
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

		rootCommand.SetAction(async (parseResult, cancellationToken) =>
		{
			var dbContextFile = parseResult.GetValue(dbContextFileArgument);
			var githubRepo = parseResult.GetValue(githubRepoOption);
			var githubToken = parseResult.GetValue(githubTokenOption);
			var output = parseResult.GetValue(outputOption);
			var guid = parseResult.GetValue(guidOption);
			var cssFile = parseResult.GetValue(cssFileOption);
			var outputDefaultCss = parseResult.GetValue(outputDefaultCssOption);
			var noOpen = parseResult.GetValue(noOpenOption);

			try
			{
				// Handle CSS export first
				if (!string.IsNullOrEmpty(outputDefaultCss))
				{
					var cssOutputPath = string.IsNullOrEmpty(outputDefaultCss) || outputDefaultCss == "true"
						? "default-schema-styles.css"
						: outputDefaultCss;

					await SchemaGenerator.ExportDefaultCssAsync(cssOutputPath);
					Console.WriteLine($"📄 Default CSS exported to: {cssOutputPath}");
					Console.WriteLine("✨ Customize this file and use --css-file to apply your changes");
					return 0;
				}

				// Handle GitHub repository
				if (!string.IsNullOrEmpty(githubRepo))
				{
					await ProcessGitHubRepositoryAsync(githubRepo, githubToken, output, cssFile, noOpen);
					return 0;
				}

				// Validate required argument for schema generation from file
				if (dbContextFile == null || !dbContextFile.Exists)
				{
					Console.WriteLine("❌ Error: Either specify a DbContext file or use --github-repo");
					Console.WriteLine();
					Console.WriteLine("📖 Quick Start Examples:");
					Console.WriteLine("   Local:  schemamagic MyDbContext.cs");
					Console.WriteLine("   GitHub: schemamagic --github-repo https://github.com/owner/repo");
					Console.WriteLine("   CSS:    schemamagic --output-default-css");
					Console.WriteLine();
					Console.WriteLine("💡 Use --help for full documentation");
					return 1;
				}

				var htmlFile = await SchemaGenerator.GenerateSchemaAsync(dbContextFile, output, guid, cssFile);
				Console.WriteLine($"📄 Schema visualization created: {htmlFile}");

				if (!string.IsNullOrEmpty(guid))
				{
					Console.WriteLine($"🆔 Document GUID: {guid} (preserves existing localStorage state)");
				}

				if (cssFile != null)
				{
					Console.WriteLine($"🎨 Custom CSS applied from: {cssFile.FullName}");
				}

				if (noOpen)
				{
					Console.WriteLine($"✨ Open manually: {htmlFile}");
				}
				else
				{
					Console.WriteLine("🌐 Opening in browser...");
					try
					{
						var psi = new ProcessStartInfo
						{
							FileName = htmlFile,
							UseShellExecute = true
						};
						Process.Start(psi);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"⚠️ Could not open browser: {ex.Message}");
						Console.WriteLine($"✨ Open manually: {htmlFile}");
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error: {ex.Message}");
				return 1;
			}
		});

		return await rootCommand.Parse(args).InvokeAsync();
	}

	private static async Task ProcessGitHubRepositoryAsync(string repoUrl, string? accessToken, string? outputPath, FileInfo? cssFile, bool noOpen)
	{
		Console.WriteLine("🚀 SchemaMagic - GitHub Repository Analysis");
		Console.WriteLine("============================================================");

		var github = new GitHubService(accessToken);
		
		// Use the new enhanced analysis method
		var analysisResult = await github.AnalyzeRepositoryAsync(repoUrl);

		if (analysisResult.DbContextFiles.Count == 0)
		{
			Console.WriteLine("❌ No DbContext files found in the repository");
			Console.WriteLine();
			Console.WriteLine("💡 Troubleshooting:");
			Console.WriteLine("   • Ensure the repository contains Entity Framework Core DbContext classes");
			Console.WriteLine("   • Check that DbContext files have 'DbSet<>' properties");
			Console.WriteLine("   • If this is a private repo, provide --github-token");
			return;
		}

		Console.WriteLine($"\n📊 Found {analysisResult.DbContextFiles.Count} DbContext file(s):");
		for (int i = 0; i < analysisResult.DbContextFiles.Count; i++)
		{
			Console.WriteLine($"   {i + 1}. {analysisResult.DbContextFiles[i].FilePath}");
		}
		
		Console.WriteLine($"\n📦 Retrieved {analysisResult.EntityFiles.Count} entity model file(s)");

		// Process each DbContext file with entity files
		foreach (var dbContextFile in analysisResult.DbContextFiles)
		{
			Console.WriteLine($"\n🔍 Analyzing: {dbContextFile.FileName}");
			
			// Generate deterministic GUID based on repository URL and file path
			var documentGuid = GenerateGuidFromRepoAndFile(repoUrl, dbContextFile.FilePath);
			Console.WriteLine($"🔑 Generated GUID: {documentGuid} (preserves layout state)");

			// Use the new analysis method with entity files
			var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
				dbContextFile.Content,
				dbContextFile.FileName,
				analysisResult.EntityFiles,
				documentGuid);

			if (!result.Success)
			{
				Console.WriteLine($"❌ Analysis failed: {result.ErrorMessage}");
				continue;
			}

			Console.WriteLine($"✅ Found {result.EntitiesFound} entities");
			
			// Show property counts for verification
			foreach (var entity in result.Entities!.Take(5))
			{
				Console.WriteLine($"   📋 {entity.Key}: {entity.Value.Properties.Count} properties");
			}
			if (result.Entities!.Count > 5)
			{
				Console.WriteLine($"   ... and {result.Entities.Count - 5} more entities");
			}

			// Determine output file path
			var outputFile = outputPath ?? Path.Combine("Output", $"{Path.GetFileNameWithoutExtension(dbContextFile.FileName)}-Schema.html");
			
			// Create output directory if needed
			var outputDir = Path.GetDirectoryName(outputFile);
			if (!string.IsNullOrEmpty(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			// Get HTML content (already generated with custom CSS if provided)
			string htmlContent;
			if (cssFile != null && cssFile.Exists)
			{
				var customCss = File.ReadAllText(cssFile.FullName);
				var entitiesJson = System.Text.Json.JsonSerializer.Serialize(result.Entities, new System.Text.Json.JsonSerializerOptions
				{
					WriteIndented = true,
					PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
				});
				htmlContent = ModularHtmlTemplate.GenerateWithCustomCss(entitiesJson, documentGuid, customCss);
				Console.WriteLine($"🎨 Applied custom CSS from: {cssFile.FullName}");
			}
			else
			{
				htmlContent = result.HtmlContent;
			}

			// Write the HTML file
			File.WriteAllText(outputFile, htmlContent);
			
			Console.WriteLine($"📄 Schema saved to: {Path.GetFullPath(outputFile)}");
			Console.WriteLine($"📏 File size: {new FileInfo(outputFile).Length / 1024:N0} KB");
			Console.WriteLine($"🔗 Repository: {dbContextFile.Repository}");
			Console.WriteLine($"📁 Source: {dbContextFile.FilePath}");

			// Open in browser (only for first file if multiple)
			if (!noOpen && analysisResult.DbContextFiles.IndexOf(dbContextFile) == 0)
			{
				Console.WriteLine("\n🌐 Opening in browser...");
				try
				{
					var psi = new ProcessStartInfo
					{
						FileName = Path.GetFullPath(outputFile),
						UseShellExecute = true
					};
					Process.Start(psi);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"⚠️ Could not open browser: {ex.Message}");
					Console.WriteLine($"💡 Open manually: {Path.GetFullPath(outputFile)}");
				}
			}
		}

		Console.WriteLine($"\n✅ Successfully processed {analysisResult.DbContextFiles.Count} DbContext file(s)");
		Console.WriteLine($"📂 All output saved to: {Path.GetFullPath("Output")}");
	}

	private static string GenerateGuidFromRepoAndFile(string repoUrl, string filePath)
	{
		// Combine repository URL and file path for uniqueness
		var combined = $"{repoUrl.Trim().ToLowerInvariant()}|{filePath.Trim().ToLowerInvariant()}";
		
		// Generate SHA256 hash
		using var sha256 = SHA256.Create();
		var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
		
		// Take first 16 bytes and convert to GUID format
		var guidBytes = new byte[16];
		Array.Copy(hashBytes, guidBytes, 16);
		
		// Set version (4) and variant bits for a valid GUID
		guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x40); // Version 4
		guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80); // Variant is 10
		
		var guid = new Guid(guidBytes);
		return guid.ToString();
	}
}