using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace SchemaMagic.Core;

public static partial class CoreSchemaAnalysisService
{
	private static readonly string[] _targetDataProjects =
	[
		"MagicSuite.Data",
		"AlertMagic.Core",
		"DataMagic.EfCore",
		"ConnectMagic",
		"DocMagic.Data"
	];

	[GeneratedRegex(@"DbSet<(.+?)>")]
	private static partial Regex DbSetTypeRegex();

	private static Dictionary<string, EntityInfo> ExtractEntitiesFromFile(string dbContextFilePath, string sourceCode)
	{
		var root = CSharpSyntaxTree.ParseText(sourceCode).GetCompilationUnitRoot();
		var contextDirectory = Path.GetDirectoryName(dbContextFilePath);
		var foreignKeyRelationships = LoadForeignKeyRelationships(contextDirectory);

		var entities = DiscoverEntities(root);

		var allClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
		allClasses.AddRange(ReadModelClasses(Path.Combine(contextDirectory ?? "", "Models")));
		allClasses.AddRange(SearchSolutionProjects(contextDirectory, entities.Keys));
		Console.WriteLine($"📊 Total classes found: {allClasses.Count}");

		PopulateEntities(entities, allClasses, foreignKeyRelationships);
		Console.WriteLine($"✅ Entity discovery complete. Found {entities.Count} entities with properties.");
		return entities;
	}

	private static Dictionary<string, EntityInfo> ExtractEntitiesWithEntityFiles(
		string dbContextContent,
		Dictionary<string, string> entityFileContents)
	{
		var root = CSharpSyntaxTree.ParseText(dbContextContent).GetCompilationUnitRoot();
		var entities = DiscoverEntities(root);

		var allClasses = new List<ClassDeclarationSyntax>();
		foreach (var (fileName, content) in entityFileContents)
		{
			var classes = TryParseClasses(content, fileName);
			allClasses.AddRange(classes);
			Console.WriteLine($"   📄 Parsed {fileName}: {classes.Count} class(es)");
		}

		Console.WriteLine($"📊 Total classes parsed: {allClasses.Count}");

		// Foreign keys come from heuristics only: there is no EF snapshot for remote sources
		PopulateEntities(entities, allClasses, []);
		Console.WriteLine($"✅ Analysis complete. Found {entities.Count} entities");
		return entities;
	}

	private static Dictionary<string, EntityInfo> DiscoverEntities(CompilationUnitSyntax root)
	{
		var dbContextClass = root.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.FirstOrDefault(c => c.BaseList?.Types.Any(t => t.ToString().Contains("DbContext")) == true)
			?? throw new InvalidOperationException("No DbContext class found in the file.");

		var dbSetProperties = dbContextClass.Members
			.OfType<PropertyDeclarationSyntax>()
			.Where(p => p.Type.ToString().StartsWith("DbSet<", StringComparison.OrdinalIgnoreCase))
			.ToList();

		Console.WriteLine($"🔍 Found {dbSetProperties.Count} DbSet properties");

		var entities = new Dictionary<string, EntityInfo>();
		foreach (var dbSetProperty in dbSetProperties)
		{
			var entityTypeName = ExtractEntityTypeName(dbSetProperty.Type.ToString());
			if (entityTypeName is not null)
			{
				entities[entityTypeName] = new EntityInfo
				{
					Type = entityTypeName,
					Properties = []
				};
			}
		}

		return entities;
	}

	private static string? ExtractEntityTypeName(string dbSetType)
	{
		var match = DbSetTypeRegex().Match(dbSetType);
		return match.Success ? match.Groups[1].Value : null;
	}

	private static List<ClassDeclarationSyntax> TryParseClasses(string sourceCode, string description)
	{
		try
		{
			return CSharpSyntaxTree.ParseText(sourceCode)
				.GetCompilationUnitRoot()
				.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.ToList();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"   ⚠️ Error parsing {description}: {ex.Message}");
			return [];
		}
	}

	private static List<ClassDeclarationSyntax> ReadModelClasses(string modelsDirectory)
	{
		var modelClasses = new List<ClassDeclarationSyntax>();
		if (!Directory.Exists(modelsDirectory))
		{
			return modelClasses;
		}

		Console.WriteLine($"📁 Looking for entity models in: {modelsDirectory}");
		foreach (var modelFile in Directory.GetFiles(modelsDirectory, "*.cs"))
		{
			modelClasses.AddRange(TryParseClasses(File.ReadAllText(modelFile), Path.GetFileName(modelFile)));
		}

		Console.WriteLine($"📊 Found {modelClasses.Count} model classes in Models directory");
		return modelClasses;
	}

	private static List<ClassDeclarationSyntax> SearchSolutionProjects(string? contextDirectory, IEnumerable<string> entityNames)
	{
		var found = new List<ClassDeclarationSyntax>();
		var solutionDirectory = FindSolutionDirectory(contextDirectory);
		if (solutionDirectory is null)
		{
			return found;
		}

		var targetNames = entityNames.ToHashSet();
		foreach (var projectName in _targetDataProjects)
		{
			var projectPath = Path.Combine(solutionDirectory, projectName);
			if (Directory.Exists(projectPath))
			{
				Console.WriteLine($"🎯 Searching targeted project: {projectName}");
				found.AddRange(SearchProjectForEntities(projectPath, targetNames));
			}
		}

		return found;
	}

	private static List<ClassDeclarationSyntax> SearchProjectForEntities(string projectPath, HashSet<string> targetNames)
	{
		var found = new List<ClassDeclarationSyntax>();
		var separator = Path.DirectorySeparatorChar;

		try
		{
			// The limit keeps very large projects from dominating the run time
			var candidateFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
				.Where(f => !f.Contains($"{separator}obj{separator}") && !f.Contains($"{separator}bin{separator}"))
				.Take(200)
				.Where(f => targetNames.Contains(Path.GetFileNameWithoutExtension(f)));

			foreach (var csFile in candidateFiles)
			{
				found.AddRange(TryParseClasses(File.ReadAllText(csFile), csFile).Where(c => targetNames.Contains(c.Identifier.Text)));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"   ⚠️ Error searching project: {ex.Message}");
		}

		Console.WriteLine($"   📊 Found {found.Count} entities in {Path.GetFileName(projectPath)}");
		return found;
	}

	private static string? FindSolutionDirectory(string? startDirectory)
	{
		if (string.IsNullOrEmpty(startDirectory))
		{
			return null;
		}

		for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
		{
			if (directory.GetFiles("*.sln").Length > 0)
			{
				return directory.FullName;
			}
		}

		return null;
	}
}
