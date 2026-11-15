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

			if (line.Contains("modelBuilder.Entity(") && line.Contains(", b =>"))
			{
				var entityMatch = SnapshotEntityRegex().Match(line);
				if (entityMatch.Success)
				{
					currentEntity = entityMatch.Groups[1].Value;
				}
			}

			if (currentEntity is null || !line.Contains(".HasForeignKey("))
			{
				continue;
			}

			var foreignKeyMatch = HasForeignKeyRegex().Match(line);
			if (foreignKeyMatch.Success)
			{
				if (!foreignKeys.TryGetValue(currentEntity, out var entityForeignKeys))
				{
					entityForeignKeys = [];
					foreignKeys[currentEntity] = entityForeignKeys;
				}

				entityForeignKeys.Add(foreignKeyMatch.Groups[1].Value);
			}
		}
	}
}
