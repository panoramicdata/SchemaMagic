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
	private readonly string? _outputPath = outputPath;
	private readonly string? _documentGuid = documentGuid;
	private readonly string? _customCssPath = customCssPath;

	public Task<string> GenerateSchemaVisualizationAsync() => Task.FromResult(GenerateSchemaVisualization());

	public static async Task ExportDefaultCssAsync(string cssOutputPath)
	{
		await Task.Run(() => OutputDefaultCss(cssOutputPath));
	}

	public static async Task<string> GenerateSchemaAsync(FileInfo dbContextFile, string? output, string? guid, FileInfo? cssFile)
	{
		var generator = new SchemaGenerator(dbContextFile, output, guid, cssFile?.FullName);
		return await generator.GenerateSchemaVisualizationAsync();
	}

	public static void OutputDefaultCss(string outputPath)
	{
		Console.WriteLine("🎨 Exporting default CSS stylesheet...");

		var outputDirectory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}

		File.WriteAllText(outputPath, ModularHtmlTemplate.GetDefaultCss());

		Console.WriteLine("📄 Default CSS exported successfully");
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
		ValidateInputs();

		Console.WriteLine("🚀 SchemaMagic - Interactive Database Schema Visualizer");
		Console.WriteLine("============================================================");
		Console.WriteLine($"🔍 Processing DbContext: {Path.GetFileName(_dbContextPath)}");

		var result = AnalyzeDbContext();
		var fullOutputPath = ResolveOutputPath();

		File.WriteAllText(fullOutputPath, BuildHtml(result));
		WriteSummary(fullOutputPath, result);

		return fullOutputPath;
	}

	private void ValidateInputs()
	{
		if (!File.Exists(_dbContextPath))
		{
			throw new FileNotFoundException($"DbContext file not found: {_dbContextPath}");
		}

		if (string.IsNullOrEmpty(_customCssPath))
		{
			return;
		}

		if (!File.Exists(_customCssPath))
		{
			throw new FileNotFoundException($"Custom CSS file not found: {_customCssPath}");
		}

		Console.WriteLine($"🎨 Using custom CSS: {Path.GetFileName(_customCssPath)}");
	}

	// The real file path lets the analysis find related model files
	private SchemaAnalysisResult AnalyzeDbContext()
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(_dbContextPath);

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

		return result;
	}

	private string ResolveOutputPath()
	{
		var contextDirectory = Path.GetDirectoryName(_dbContextPath) ?? Directory.GetCurrentDirectory();
		var outputDirectory = Path.Combine(contextDirectory, "Output");
		Directory.CreateDirectory(outputDirectory);

		var fileName = _outputPath ?? $"{Path.GetFileNameWithoutExtension(_dbContextPath)}-Schema.html";
		if (!fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
		{
			fileName += ".html";
		}

		return Path.IsPathRooted(fileName) ? fileName : Path.Combine(outputDirectory, Path.GetFileName(fileName));
	}

	private string BuildHtml(SchemaAnalysisResult result)
	{
		var hasCustomCss = !string.IsNullOrEmpty(_customCssPath);
		if (!hasCustomCss && string.IsNullOrEmpty(_documentGuid))
		{
			return result.HtmlContent;
		}

		var entitiesJson = JsonSerializer.Serialize(result.Entities, _jsonSerializerOptions);
		if (hasCustomCss)
		{
			var customCss = File.ReadAllText(_customCssPath!);
			return ModularHtmlTemplate.GenerateWithCustomCss(entitiesJson, _documentGuid ?? Guid.NewGuid().ToString(), customCss);
		}

		return ModularHtmlTemplate.Generate(entitiesJson, _documentGuid);
	}

	private void WriteSummary(string fullOutputPath, SchemaAnalysisResult result)
	{
		Console.WriteLine("✅ Interactive schema visualization generated!");
		Console.WriteLine($"📄 Output file: {fullOutputPath}");
		Console.WriteLine($"🆔 Document GUID: {_documentGuid ?? result.DocumentGuid}");

		Console.WriteLine(string.IsNullOrEmpty(_documentGuid)
			? "🆕 New document - will auto-optimize layout on first load"
			: "🔄 Using provided GUID - will preserve existing localStorage state");

		if (!string.IsNullOrEmpty(_customCssPath))
		{
			Console.WriteLine($"🎨 Custom styling applied from: {Path.GetFileName(_customCssPath)}");
		}

		Console.WriteLine("📊 Features: Drag tables, Pan background, Zoom controls, Toggle relations");
	}
}
