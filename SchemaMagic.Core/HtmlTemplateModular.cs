using System.Reflection;
using System.Text;

namespace SchemaMagic.Core;

public static class ModularHtmlTemplate
{
	private static readonly Dictionary<string, string> _templateCache = [];

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

	// Static method to generate HTML content with custom CSS string
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
			Console.WriteLine("🔄 Falling back to default styles");
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

		// First try to load from Templates directory (capital T) relative to current location
		var currentDir = Directory.GetCurrentDirectory();
		var templatesPath = Path.Combine(currentDir, "Templates", fileName);

		if (File.Exists(templatesPath))
		{
			var content = File.ReadAllText(templatesPath);
			_templateCache[fileName] = content;
			return content;
		}

		// Try lowercase templates directory
		var templatesPath2 = Path.Combine(currentDir, "templates", fileName);

		if (File.Exists(templatesPath2))
		{
			var content = File.ReadAllText(templatesPath2);
			_templateCache[fileName] = content;
			return content;
		}

		// Try relative to the assembly location
		var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
		var templatesPath3 = Path.Combine(assemblyDir, "..", "..", "..", "..", "Templates", fileName);

		if (File.Exists(templatesPath3))
		{
			var content = File.ReadAllText(templatesPath3);
			_templateCache[fileName] = content;
			return content;
		}

		// Fallback: look up from current directory
		var templatesPath4 = Path.Combine("..", "Templates", fileName);
		if (File.Exists(templatesPath4))
		{
			var content = File.ReadAllText(templatesPath4);
			_templateCache[fileName] = content;
			return content;
		}

		throw new FileNotFoundException($"Template file not found: {fileName} (searched in Templates/, templates/, and assembly-relative paths)");
	}
}