using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SchemaMagic.Core;

/// <summary>
/// Generates self-contained HTML schema documents from embedded templates.
/// </summary>
public static class ModularHtmlTemplate
{
	private const string DefaultStylesHeader = "/* ========== DEFAULT STYLES ========== */";
	private const string CustomStylesHeader = "/* ========== CUSTOM OVERRIDES ========== */";

	private static readonly ConcurrentDictionary<string, string> _templateCache = [];

	private static readonly string[] _javaScriptFiles =
	[
		"variables.js",
		"event-listeners.js",
		"pan-zoom.js",
		"settings.js",
		"layout-metrics.js",
		"force-directed-layout.js",
		"schema-generation.js",
		"table-generation.js",
		"property-utilities.js",
		"table-interaction.js",
		"relationships.js",
		"table-grouping.js",
		"controls.js"
	];

	/// <summary>
	/// Generates a schema document with a generated identifier and the default styles.
	/// </summary>
	/// <param name="entitiesJson">The serialized entity metadata.</param>
	/// <returns>The generated HTML document.</returns>
	public static string Generate(string entitiesJson) => Generate(entitiesJson, null, null);

	/// <summary>
	/// Generates a schema document with the default styles.
	/// </summary>
	/// <param name="entitiesJson">The serialized entity metadata.</param>
	/// <param name="documentGuid">An optional identifier used to scope document state.</param>
	/// <returns>The generated HTML document.</returns>
	public static string Generate(string entitiesJson, string? documentGuid) => Generate(entitiesJson, documentGuid, null);

	/// <summary>
	/// Generates a schema document, optionally applying CSS from a file.
	/// </summary>
	/// <param name="entitiesJson">The serialized entity metadata.</param>
	/// <param name="documentGuid">An optional identifier used to scope document state.</param>
	/// <param name="customCssPath">An optional path to a CSS file whose rules override the defaults.</param>
	/// <returns>The generated HTML document.</returns>
	public static string Generate(string entitiesJson, string? documentGuid, string? customCssPath) =>
		Render(entitiesJson, documentGuid ?? Guid.NewGuid().ToString(), GetMergedCss(customCssPath));

	/// <summary>
	/// Generates a schema document with CSS overrides supplied as text.
	/// </summary>
	/// <param name="entitiesJson">The serialized entity metadata.</param>
	/// <param name="documentGuid">The identifier used to scope document state.</param>
	/// <param name="customCss">Optional CSS rules that override the defaults.</param>
	/// <returns>The generated HTML document.</returns>
	public static string GenerateWithCustomCss(string entitiesJson, string documentGuid, string? customCss) =>
		Render(
			entitiesJson,
			documentGuid,
			string.IsNullOrEmpty(customCss) ? GetDefaultCss() : MergeCss(GetDefaultCss(), customCss, null));

	/// <summary>
	/// Gets the default stylesheet embedded in the library.
	/// </summary>
	/// <returns>The default CSS content.</returns>
	public static string GetDefaultCss() => LoadTemplate("styles.css");

	private static string Render(string entitiesJson, string documentGuid, string css) =>
		LoadTemplate("template.html")
			.Replace("<!-- CSS STYLES -->", $"<style>\n{css}\n</style>")
			.Replace("<!-- JAVASCRIPT CONTENT -->", CombineJavaScriptFiles(entitiesJson, documentGuid));

	private static string MergeCss(string defaultCss, string customCss, string? sourceName)
	{
		var mergedCss = new StringBuilder();
		mergedCss.AppendLine(DefaultStylesHeader);
		mergedCss.AppendLine(defaultCss);
		mergedCss.AppendLine();
		mergedCss.AppendLine(CustomStylesHeader);
		if (sourceName is not null)
		{
			mergedCss.AppendLine($"/* Loaded from: {sourceName} */");
		}

		mergedCss.AppendLine(customCss);
		return mergedCss.ToString();
	}

	private static string GetMergedCss(string? customCssPath)
	{
		var defaultCss = GetDefaultCss();

		if (string.IsNullOrEmpty(customCssPath) || !File.Exists(customCssPath))
		{
			return defaultCss;
		}

		try
		{
			return MergeCss(defaultCss, File.ReadAllText(customCssPath), Path.GetFileName(customCssPath));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️ Warning: Could not load custom CSS file '{customCssPath}': {ex.Message}");
			Console.WriteLine("🔄 Falling back to default styles");
			return defaultCss;
		}
	}

	private static string CombineJavaScriptFiles(string entitiesJson, string documentGuid)
	{
		// The GUID is emitted as a JSON string literal so it can never end the literal early
		var guidLiteral = JsonSerializer.Serialize(documentGuid);

		return string.Join("\n\n", _javaScriptFiles.Select(LoadTemplate))
			.Replace("ENTITIES_JSON_PLACEHOLDER", entitiesJson)
			.Replace("\"DOCUMENT_GUID_PLACEHOLDER\"", guidLiteral);
	}

	private static string LoadTemplate(string fileName) =>
		_templateCache.GetOrAdd(fileName, LoadTemplateUncached);

	private static string LoadTemplateUncached(string fileName)
	{
		var assembly = Assembly.GetExecutingAssembly();

		// Embedded resources work in Blazor WASM; the file system is the command-line fallback
		using var stream = assembly.GetManifestResourceStream($"SchemaMagic.Core.Templates.{fileName}");
		if (stream is not null)
		{
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		var content = TryReadTemplateFromFileSystem(assembly, fileName);
		if (content is not null)
		{
			return content;
		}

		Console.WriteLine("📋 Available embedded resources:");
		foreach (var resourceName in assembly.GetManifestResourceNames())
		{
			Console.WriteLine($"   - {resourceName}");
		}

		throw new FileNotFoundException($"Template file not found: {fileName} (searched embedded resources and file system)");
	}

	private static string? TryReadTemplateFromFileSystem(Assembly assembly, string fileName)
	{
		var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? "";
		string[] pathsToTry =
		[
			Path.Combine(Directory.GetCurrentDirectory(), "Templates", fileName),
			Path.Combine(Directory.GetCurrentDirectory(), "templates", fileName),
			Path.Combine(assemblyDirectory, "..", "..", "..", "..", "Templates", fileName),
			Path.Combine("..", "Templates", fileName)
		];

		var path = pathsToTry.FirstOrDefault(File.Exists);
		return path is null ? null : File.ReadAllText(path);
	}
}
