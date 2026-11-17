using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace SchemaMagic.Core;

/// <summary>
/// Source inspection shared by the command-line tool and the web application.
/// </summary>
internal static partial class GitHubSourceHelpers
{
	[GeneratedRegex(@"DbSet<(.+?)>")]
	private static partial Regex DbSetRegex();

	public static (string Owner, string Name)? ParseRepositoryUrl(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != "github.com")
		{
			return null;
		}

		var segments = uri.AbsolutePath.Trim('/').Split('/');
		return segments.Length >= 2 ? (segments[0], segments[1]) : null;
	}

	public static bool IsDbContextFile(string content) =>
		content.Contains("DbContext", StringComparison.OrdinalIgnoreCase)
		&& (content.Contains("class", StringComparison.OrdinalIgnoreCase)
			|| content.Contains("public", StringComparison.OrdinalIgnoreCase))
		&& content.Contains("DbSet", StringComparison.OrdinalIgnoreCase);

	public static bool IsDbContextCandidatePath(string path) =>
		path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
		&& path.Contains("Context", StringComparison.OrdinalIgnoreCase);

	public static List<string> ExtractEntityNames(string dbContextContent)
	{
		var dbContextClass = CSharpSyntaxTree.ParseText(dbContextContent)
			.GetCompilationUnitRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.FirstOrDefault(c => c.BaseList?.Types.Any(t => t.ToString().Contains("DbContext")) == true);

		if (dbContextClass is null)
		{
			return [];
		}

		return dbContextClass.Members
			.OfType<PropertyDeclarationSyntax>()
			.Select(p => p.Type.ToString())
			.Where(type => type.StartsWith("DbSet<", StringComparison.OrdinalIgnoreCase))
			.Select(type => DbSetRegex().Match(type))
			.Where(match => match.Success)
			.Select(match => match.Groups[1].Value.Trim())
			.ToList();
	}

	public static bool ContainsClassDefinition(string content, string className) =>
		CSharpSyntaxTree.ParseText(content)
			.GetCompilationUnitRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.Any(c => c.Identifier.Text == className);

	// Entity files are searched in the likeliest folders first
	public static int EntityDirectoryPriority(string path)
	{
		string[] priorityDirectories = ["/models/", "/entities/", "/domain/", "/data/"];
		var lowered = path.ToLowerInvariant();
		var index = Array.FindIndex(priorityDirectories, lowered.Contains);
		return index < 0 ? priorityDirectories.Length : index;
	}
}
