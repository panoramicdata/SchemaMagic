using SchemaMagic.Core;
using System.Text.Json;

namespace SchemaMagic;

public partial class SchemaGenerator(FileInfo dbContextFile, string? outputPath = null, string? documentGuid = null, string? customCssPath = null)
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly string _dbContextPath = dbContextFile.FullName;
	private readonly string? _documentGuid = documentGuid;
	private readonly string? _customCssPath = customCssPath;

	public Task<string> GenerateSchemaVisualizationAsync() => Task.FromResult(GenerateSchemaVisualization());

	// Static async methods for Program.cs
	public static async Task ExportDefaultCssAsync(string cssOutputPath)
	{
		await Task.Run(() => OutputDefaultCss(cssOutputPath));
	}

	public static async Task<string> GenerateSchemaAsync(FileInfo dbContextFile, string? output, string? guid, FileInfo? cssFile)
	{
		var cssPath = cssFile?.FullName;
		var generator = new SchemaGenerator(dbContextFile, output, guid, cssPath);
		return await generator.GenerateSchemaVisualizationAsync();
	}

	public static void OutputDefaultCss(string outputPath)
	{
		Console.WriteLine("🎨 Exporting default CSS stylesheet...");

		// Ensure output directory exists
		var outputDirectory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}

		// Get the default CSS content
		var defaultCss = ModularHtmlTemplate.GetDefaultCss();

		// Write to file
		File.WriteAllText(outputPath, defaultCss);

		Console.WriteLine($"📄 Default CSS exported successfully");
		Console.WriteLine($"📂 Location: {Path.GetFullPath(outputPath)}");
		Console.WriteLine($"📏 Size: {new FileInfo(outputPath).Length:N0} bytes");
		Console.WriteLine();
		Console.WriteLine("💡 Usage Tips:");
		Console.WriteLine("   • Modify colors, fonts, sizes, and layout in this file");
		Console.WriteLine("   • Use --css-file path/to/your-styles.css to apply customizations");
		Console.WriteLine("   • Your changes will override the default styling");
		Console.WriteLine("   • Invalid CSS will be ignored, falling back to defaults");
	}

	public string GenerateSchemaVisualization()
	{
		if (!File.Exists(_dbContextPath))
		{
			throw new FileNotFoundException($"DbContext file not found: {_dbContextPath}");
		}

		// Validate custom CSS file if provided
		if (!string.IsNullOrEmpty(_customCssPath))
		{
			if (!File.Exists(_customCssPath))
			{
				throw new FileNotFoundException($"Custom CSS file not found: {_customCssPath}");
			}

			Console.WriteLine($"🎨 Using custom CSS: {Path.GetFileName(_customCssPath)}");
		}

		Console.WriteLine("🚀 SchemaMagic - Interactive Database Schema Visualizer");
		Console.WriteLine("============================================================");
		Console.WriteLine($"🔍 Processing DbContext: {Path.GetFileName(_dbContextPath)}");

		// Read DbContext content
		var sourceCode = File.ReadAllText(_dbContextPath);
		var fileName = Path.GetFileName(_dbContextPath);

		// Use the Core library for analysis with the sophisticated engine
		var result = CoreSchemaAnalysisService.AnalyzeDbContextContent(sourceCode, fileName);

		if (!result.Success)
		{
			Console.WriteLine($"❌ Analysis failed: {result.ErrorMessage}");
			throw new InvalidOperationException($"Schema analysis failed: {result.ErrorMessage}");
		}

		Console.WriteLine($"📊 Found {result.EntitiesFound} entities");

		if (result.EntitiesFound == 0)
		{
			Console.WriteLine("❌ No entities found in the DbContext file.");
			throw new InvalidOperationException("No entities found in the DbContext file.");
		}

		// Determine output path
		var contextDirectory = Path.GetDirectoryName(_dbContextPath) ?? Directory.GetCurrentDirectory();
		var outputDirectory = Path.Combine(contextDirectory, "Output");
		Directory.CreateDirectory(outputDirectory);

		var fileName2 = outputPath ?? $"{Path.GetFileNameWithoutExtension(_dbContextPath)}-Schema.html";
		if (!fileName2.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
		{
			fileName2 += ".html";
		}

		var fullOutputPath = Path.IsPathRooted(fileName2) ? fileName2 : Path.Combine(outputDirectory, Path.GetFileName(fileName2));

		// Generate HTML with custom CSS if provided
		string htmlContent;
		if (!string.IsNullOrEmpty(_customCssPath))
		{
			var customCss = File.ReadAllText(_customCssPath);
			var entitiesJson = JsonSerializer.Serialize(result.Entities, _jsonSerializerOptions);
			var documentGuid = _documentGuid ?? Guid.NewGuid().ToString();
			htmlContent = ModularHtmlTemplate.Generate(entitiesJson, documentGuid, customCss);
		}
		else
		{
			// Use the provided GUID if specified, otherwise use the one from the result
			if (!string.IsNullOrEmpty(_documentGuid))
			{
				var entitiesJson = JsonSerializer.Serialize(result.Entities, _jsonSerializerOptions);
				htmlContent = ModularHtmlTemplate.Generate(entitiesJson, _documentGuid, null);
			}
			else
			{
				htmlContent = result.HtmlContent;
			}
		}

		// Write the HTML file
		File.WriteAllText(fullOutputPath, htmlContent);

		Console.WriteLine("✅ Interactive schema visualization generated!");
		Console.WriteLine($"📄 Output file: {fullOutputPath}");
		
		var displayGuid = _documentGuid ?? result.DocumentGuid;
		Console.WriteLine($"🆔 Document GUID: {displayGuid}");
		
		if (!string.IsNullOrEmpty(_documentGuid))
		{
			Console.WriteLine("🔄 Using provided GUID - will preserve existing localStorage state");
		}
		else
		{
			Console.WriteLine("🆕 New document - will auto-optimize layout on first load");
		}

		if (!string.IsNullOrEmpty(_customCssPath))
		{
			Console.WriteLine($"🎨 Custom styling applied from: {Path.GetFileName(_customCssPath)}");
		}

		Console.WriteLine("📊 Features: Drag tables, Pan background, Zoom controls, Toggle relations");

		return fullOutputPath;
	}
}
