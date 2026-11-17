using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace SchemaMagic.Core;

public static partial class CoreSchemaAnalysisService
{
	[GeneratedRegex(@"\.HasForeignKey\(""([^""]+)""\)")]
	private static partial Regex HasForeignKeyRegex();

	[GeneratedRegex(@"Entity\(""[^""]*\.([^"".]+)""")]
	private static partial Regex SnapshotEntityRegex();

	private static Dictionary<string, List<string>> LoadForeignKeyRelationships(string? contextDirectory)
	{
		var migrationDirectory = Path.Combine(contextDirectory ?? "", "Migrations");
		var snapshotPath = FindBestModelSnapshot(migrationDirectory);

		if (snapshotPath is null || !File.Exists(snapshotPath))
		{
			Console.WriteLine("⚠️ No EF Model Snapshot found - using heuristic FK detection");
			return [];
		}

		Console.WriteLine($"📋 Found EF Snapshot: {Path.GetFileName(snapshotPath)}");
		var foreignKeys = ExtractForeignKeysFromSnapshot(snapshotPath);
		Console.WriteLine($"🔗 Extracted {foreignKeys.Sum(kvp => kvp.Value.Count)} foreign key relationships");
		return foreignKeys;
	}

	private static string? FindBestModelSnapshot(string migrationDirectory)
	{
		if (!Directory.Exists(migrationDirectory))
		{
			Console.WriteLine($"📁 Migration directory not found: {migrationDirectory}");
			return null;
		}

		// Provider-specific subdirectories are preferred over the root directory
		var directories = Directory.GetDirectories(migrationDirectory).Append(migrationDirectory);
		return directories
			.Select(directory => Directory.GetFiles(directory, "*ModelSnapshot.cs").FirstOrDefault())
			.FirstOrDefault(snapshot => snapshot is not null);
	}

	private static Dictionary<string, List<string>> ExtractForeignKeysFromSnapshot(string snapshotPath)
	{
		var foreignKeys = new Dictionary<string, List<string>>();

		try
		{
			var snapshotContent = File.ReadAllText(snapshotPath);
			var root = CSharpSyntaxTree.ParseText(snapshotContent).GetCompilationUnitRoot();
			var hasBuildModel = root.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Any(m => m.Identifier.Text == "BuildModel");

			if (hasBuildModel)
			{
				CollectSnapshotForeignKeys(snapshotContent, foreignKeys);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️ Warning: Could not parse EF snapshot: {ex.Message}");
		}

		return foreignKeys;
	}

	private static void CollectSnapshotForeignKeys(string snapshotContent, Dictionary<string, List<string>> foreignKeys)
	{
		string? currentEntity = null;

		foreach (var rawLine in snapshotContent.Split('\n'))
		{
			var line = rawLine.Trim();
			currentEntity = ReadSnapshotEntity(line) ?? currentEntity;

			if (currentEntity is not null && line.Contains(".HasForeignKey("))
			{
				AddSnapshotForeignKey(line, currentEntity, foreignKeys);
			}
		}
	}

	private static string? ReadSnapshotEntity(string line)
	{
		if (!line.Contains("modelBuilder.Entity(") || !line.Contains(", b =>"))
		{
			return null;
		}

		var entityMatch = SnapshotEntityRegex().Match(line);
		return entityMatch.Success ? entityMatch.Groups[1].Value : null;
	}

	private static void AddSnapshotForeignKey(string line, string entity, Dictionary<string, List<string>> foreignKeys)
	{
		var foreignKeyMatch = HasForeignKeyRegex().Match(line);
		if (!foreignKeyMatch.Success)
		{
			return;
		}

		if (!foreignKeys.TryGetValue(entity, out var entityForeignKeys))
		{
			entityForeignKeys = [];
			foreignKeys[entity] = entityForeignKeys;
		}

		entityForeignKeys.Add(foreignKeyMatch.Groups[1].Value);
	}
}
