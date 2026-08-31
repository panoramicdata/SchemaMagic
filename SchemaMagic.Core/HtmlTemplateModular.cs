using System.Reflection;
using System.Text;
using System.Collections.Concurrent;

namespace SchemaMagic.Core;

/// <summary>
/// Generates self-contained HTML schema documents from embedded templates.
/// </summary>
public static class ModularHtmlTemplate
{
	private static readonly ConcurrentDictionary<string, string> _templateCache = [];

	/// <summary>
	/// Generates a schema document, optionally applying CSS from a file.
	/// </summary>
	/// <param name="entitiesJson">The serialized entity metadata.</param>
	/// <param name="documentGuid">An optional identifier used to scope document state.</param>
	/// <param name="customCssPath">An optional path to a CSS file whose rules override the defaults.</param>
	/// <returns>The generated HTML document.</returns>
	public static string Generate(string entitiesJson, string? documentGuid = null, string? customCssPath = null)
	{
		// Generate a new GUID if none provided
		var guid = documentGuid ?? Guid.NewGuid().ToString();

		var html = LoadTemplate("template.html");
		var css = GetMergedCss(customCssPath);
		var javascript = CombineJavaScriptFiles(entitiesJson, guid);

		// Replace placeholders
		html = html.Replace("<!-- CSS STYLES -->", $"<style>\n{css}\n</style>");
		html = html.Replace("<!-- JAVASCRIPT CONTENT -->", javascript);

		return html;
	}

	/// <summary>
	/// Generates a schema document with CSS overrides supplied as text.
	/// </summary>
	/// <param name="entitiesJson">The serialized entity metadata.</param>
	/// <param name="documentGuid">The identifier used to scope document state.</param>
	/// <param name="customCss">Optional CSS rules that override the defaults.</param>
	/// <returns>The generated HTML document.</returns>
	public static string GenerateWithCustomCss(string entitiesJson, string documentGuid, string? customCss)
	{
		var html = LoadTemplate("template.html");
		var css = string.IsNullOrEmpty(customCss) ? GetDefaultCss() : GetMergedCssFromString(customCss);
		var javascript = CombineJavaScriptFiles(entitiesJson, documentGuid);

		// Replace placeholders
		html = html.Replace("<!-- CSS STYLES -->", $"<style>\n{css}\n</style>");
		html = html.Replace("<!-- JAVASCRIPT CONTENT -->", javascript);

		return html;
	}

	/// <summary>
	/// Gets the default stylesheet embedded in the library.
	/// </summary>
	/// <returns>The default CSS content.</returns>
	public static string GetDefaultCss()
	{
		return LoadTemplate("styles.css");
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
			var customCss = File.ReadAllText(customCssPath);
			var mergedCss = new StringBuilder();

			// Start with default styles
			mergedCss.AppendLine("/* ========== DEFAULT STYLES ========== */");
			mergedCss.AppendLine(defaultCss);
			mergedCss.AppendLine();

			// Add custom overrides
			mergedCss.AppendLine("/* ========== CUSTOM OVERRIDES ========== */");
			mergedCss.AppendLine($"/* Loaded from: {Path.GetFileName(customCssPath)} */");
			mergedCss.AppendLine(customCss);

			Console.WriteLine($"🎨 CSS merged successfully:");
			Console.WriteLine($"   Default styles: {defaultCss.Length:N0} characters");
			Console.WriteLine($"   Custom styles: {customCss.Length:N0} characters");
			Console.WriteLine($"   Total merged: {mergedCss.Length:N0} characters");

			return mergedCss.ToString();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️ Warning: Could not load custom CSS file '{customCssPath}': {ex.Message}");
			Console.WriteLine($"🔄 Falling back to default styles");
			return defaultCss;
		}
	}

	private static string GetMergedCssFromString(string customCss)
	{
		var defaultCss = GetDefaultCss();
		var mergedCss = new StringBuilder();

		// Start with default styles
		mergedCss.AppendLine("/* ========== DEFAULT STYLES ========== */");
		mergedCss.AppendLine(defaultCss);
		mergedCss.AppendLine();

		// Add custom overrides
		mergedCss.AppendLine("/* ========== CUSTOM OVERRIDES ========== */");
		mergedCss.AppendLine(customCss);

		return mergedCss.ToString();
	}

	private static string CombineJavaScriptFiles(string entitiesJson, string documentGuid, Dictionary<string, object>? embeddedState = null)
	{
		var jsFiles = new[]
		{
			"variables.js",
			"event-listeners.js",
			"pan-zoom.js",
			"settings.js",
			"force-directed-layout.js",
			"schema-generation.js",
			"table-generation.js",
			"property-utilities.js",
			"table-interaction.js",
			"relationships.js",
			"table-grouping.js",  // Add table grouping controls
			"controls.js"
		};

		var combinedJs = string.Join("\n\n", jsFiles.Select(LoadTemplate));

		// Replace the entities JSON placeholder
		combinedJs = combinedJs.Replace("ENTITIES_JSON_PLACEHOLDER", entitiesJson);

		// Replace the document GUID placeholder  
		combinedJs = combinedJs.Replace("DOCUMENT_GUID_PLACEHOLDER", documentGuid);

		// If we have embedded state (for download), inject it
		if (embeddedState != null)
		{
			var stateScript = GenerateEmbeddedStateScript(embeddedState, documentGuid);
			combinedJs += "\n\n" + stateScript;
		}

		return combinedJs;
	}

	private static string GenerateEmbeddedStateScript(Dictionary<string, object> state, string documentGuid)
	{
		var script = @"
// Embedded state for downloaded document
// This acts as a proxy for localStorage to preserve user customizations
(function() {
	const embeddedState = " + System.Text.Json.JsonSerializer.Serialize(state) + @";
	const originalGuid = '" + documentGuid + @"';

	// Override localStorage for this document's keys
	const originalGetItem = localStorage.getItem.bind(localStorage);
	const originalSetItem = localStorage.setItem.bind(localStorage);

	localStorage.getItem = function(key) {
		// Check if this is one of our document-specific keys
		if (key.startsWith(`schemaMagic_${originalGuid}_`)) {
			const keyType = key.replace(`schemaMagic_${originalGuid}_`, '');
			return embeddedState[keyType] || null;
		}
		return originalGetItem(key);
	};

	localStorage.setItem = function(key, value) {
		// Allow normal localStorage operation for the new document GUID
		originalSetItem(key, value);
	};

	console.log('📦 Downloaded document with embedded state from original document');
})();";

		return script;
	}

	private static string LoadTemplate(string fileName)
	{
		if (_templateCache.TryGetValue(fileName, out var cached))
		{
			return cached;
		}

		// Try to load from embedded resources first (works in Blazor WASM)
		var assembly = Assembly.GetExecutingAssembly();
		var resourceName = $"SchemaMagic.Core.Templates.{fileName}";

		Console.WriteLine($"🔍 Looking for embedded resource: {resourceName}");

		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream != null)
		{
			using var reader = new StreamReader(stream);
			var template = reader.ReadToEnd();
			_templateCache[fileName] = template;
			Console.WriteLine($"✅ Loaded from embedded resource: {fileName} ({template.Length} chars)");
			return template;
		}

		// Fallback to file system (for command-line tool)
		Console.WriteLine($"⚠️ Embedded resource not found, trying file system...");

		// Try various file system paths
		var pathsToTry = new[]
		{
			Path.Combine(Directory.GetCurrentDirectory(), "Templates", fileName),
			Path.Combine(Directory.GetCurrentDirectory(), "templates", fileName),
			Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "..", "..", "..", "..", "Templates", fileName),
			Path.Combine("..", "Templates", fileName)
		};

		foreach (var path in pathsToTry)
		{
			if (File.Exists(path))
			{
				var content = File.ReadAllText(path);
				_templateCache[fileName] = content;
				Console.WriteLine($"✅ Loaded from file system: {path}");
				return content;
			}
		}

		// List all embedded resources to help debug
		Console.WriteLine("📋 Available embedded resources:");
		foreach (var res in assembly.GetManifestResourceNames())
		{
			Console.WriteLine($"   - {res}");
		}

		throw new FileNotFoundException($"Template file not found: {fileName} (searched embedded resources and file system)");
	}
}
