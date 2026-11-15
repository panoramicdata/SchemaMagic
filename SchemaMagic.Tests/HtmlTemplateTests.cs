using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

public class HtmlTemplateTests
{
	private const string EntitiesJson = """{"Widget":{"type":"Widget","properties":[]}}""";
	private const string Guid1 = "11111111-1111-4111-8111-111111111111";

	[Fact]
	public void DefaultCss_IsNotEmpty() =>
		Assert.False(string.IsNullOrWhiteSpace(ModularHtmlTemplate.GetDefaultCss()));

	[Fact]
	public void Generate_EmbedsEntitiesAndGuid()
	{
		var html = ModularHtmlTemplate.Generate(EntitiesJson, Guid1);

		Assert.Contains(EntitiesJson, html);
		Assert.Contains($"const DOCUMENT_GUID = \"{Guid1}\";", html);
		Assert.DoesNotContain("ENTITIES_JSON_PLACEHOLDER", html);
		Assert.DoesNotContain("<!-- CSS STYLES -->", html);
		Assert.DoesNotContain("<!-- JAVASCRIPT CONTENT -->", html);
	}

	[Fact]
	public void Generate_WithoutGuid_GeneratesOne()
	{
		var first = ModularHtmlTemplate.Generate(EntitiesJson);
		var second = ModularHtmlTemplate.Generate(EntitiesJson, null);

		Assert.DoesNotContain("DOCUMENT_GUID_PLACEHOLDER", first);
		Assert.NotEqual(first, second);
	}

	[Fact]
	public void Generate_GuidWithQuotes_CannotBreakOutOfTheStringLiteral()
	{
		const string hostile = "x\"; alert(1); //";

		var html = ModularHtmlTemplate.Generate(EntitiesJson, hostile);

		Assert.Contains("const DOCUMENT_GUID = \"x\\u0022; alert(1); //\";", html);
	}

	[Fact]
	public void Generate_WithCssFile_MergesCustomStylesAfterDefaults()
	{
		using var directory = new TempDirectory();
		var cssPath = directory.Write("custom.css", ".custom-marker { color: red; }");

		var html = ModularHtmlTemplate.Generate(EntitiesJson, Guid1, cssPath);

		var defaultsAt = html.IndexOf("DEFAULT STYLES", StringComparison.Ordinal);
		var customAt = html.IndexOf("CUSTOM OVERRIDES", StringComparison.Ordinal);
		Assert.True(defaultsAt >= 0 && customAt > defaultsAt);
		Assert.Contains("Loaded from: custom.css", html);
		Assert.Contains(".custom-marker", html);
	}

	[Fact]
	public void Generate_WithMissingCssFile_UsesDefaultStyles()
	{
		var html = ModularHtmlTemplate.Generate(EntitiesJson, Guid1, Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".css"));

		Assert.DoesNotContain("CUSTOM OVERRIDES", html);
	}

	[Fact]
	public void GenerateWithCustomCss_AppendsTheSuppliedRules()
	{
		var html = ModularHtmlTemplate.GenerateWithCustomCss(EntitiesJson, Guid1, ".from-text { color: blue; }");

		Assert.Contains("CUSTOM OVERRIDES", html);
		Assert.Contains(".from-text", html);
		Assert.Contains($"\"{Guid1}\"", html);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void GenerateWithCustomCss_WithoutRules_UsesDefaultStyles(string? customCss)
	{
		var html = ModularHtmlTemplate.GenerateWithCustomCss(EntitiesJson, Guid1, customCss);

		Assert.DoesNotContain("CUSTOM OVERRIDES", html);
	}
}
