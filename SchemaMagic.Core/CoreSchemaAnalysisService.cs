using System.Text.Json;

namespace SchemaMagic.Core;

/// <summary>
/// Analyzes Entity Framework DbContext source code and generates an interactive schema document.
/// </summary>
public static partial class CoreSchemaAnalysisService
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	/// <summary>
	/// Analyzes DbContext source code supplied as text.
	/// </summary>
	/// <param name="dbContextContent">The DbContext source code.</param>
	/// <param name="fileName">The source file name or full path.</param>
	/// <returns>The schema analysis result.</returns>
	public static SchemaAnalysisResult AnalyzeDbContextContent(string dbContextContent, string fileName) =>
		RunAnalysis(() => ExtractEntitiesFromContent(dbContextContent, fileName), null);

	/// <summary>
	/// Analyzes a DbContext source file and related entity files discoverable from its location.
	/// </summary>
	/// <param name="dbContextFilePath">The full path to the DbContext source file.</param>
	/// <returns>The schema analysis result.</returns>
	public static SchemaAnalysisResult AnalyzeDbContextFile(string dbContextFilePath) =>
		RunAnalysis(
			() =>
			{
				if (!File.Exists(dbContextFilePath))
				{
					throw new FileNotFoundException($"DbContext file not found: {dbContextFilePath}");
				}

				return ExtractEntitiesFromFile(dbContextFilePath, File.ReadAllText(dbContextFilePath));
			},
			null);

	/// <summary>
	/// Analyzes DbContext source code together with an explicit collection of entity source files.
	/// </summary>
	/// <param name="dbContextContent">The DbContext source code.</param>
	/// <param name="dbContextFileName">The DbContext source file name.</param>
	/// <param name="entityFileContents">Entity source code keyed by file name.</param>
	/// <returns>The schema analysis result.</returns>
	public static SchemaAnalysisResult AnalyzeDbContextWithEntityFiles(
		string dbContextContent,
		string dbContextFileName,
		Dictionary<string, string> entityFileContents) =>
		AnalyzeDbContextWithEntityFiles(dbContextContent, dbContextFileName, entityFileContents, null);

	/// <summary>
	/// Analyzes DbContext source code together with an explicit collection of entity source files.
	/// </summary>
	/// <param name="dbContextContent">The DbContext source code.</param>
	/// <param name="dbContextFileName">The DbContext source file name.</param>
	/// <param name="entityFileContents">Entity source code keyed by file name.</param>
	/// <param name="documentGuid">An optional identifier used to scope document state.</param>
	/// <returns>The schema analysis result.</returns>
	public static SchemaAnalysisResult AnalyzeDbContextWithEntityFiles(
		string dbContextContent,
		string dbContextFileName,
		Dictionary<string, string> entityFileContents,
		string? documentGuid) =>
		RunAnalysis(
			() =>
			{
				Console.WriteLine($"🔍 Analyzing DbContext with {entityFileContents.Count} entity files");
				return ExtractEntitiesWithEntityFiles(dbContextContent, entityFileContents);
			},
			documentGuid);

	private static SchemaAnalysisResult RunAnalysis(Func<Dictionary<string, EntityInfo>> extract, string? documentGuid)
	{
		try
		{
			var entities = extract();
			var guid = documentGuid ?? Guid.NewGuid().ToString();
			var entitiesJson = JsonSerializer.Serialize(entities, _jsonSerializerOptions);

			return new SchemaAnalysisResult
			{
				Success = true,
				HtmlContent = ModularHtmlTemplate.Generate(entitiesJson, guid, null),
				EntitiesFound = entities.Count,
				DocumentGuid = guid,
				Entities = entities
			};
		}
		catch (Exception ex)
		{
			return new SchemaAnalysisResult
			{
				Success = false,
				ErrorMessage = ex.Message,
				EntitiesFound = 0
			};
		}
	}

	private static Dictionary<string, EntityInfo> ExtractEntitiesFromContent(string dbContextContent, string fileName)
	{
		if (Path.IsPathRooted(fileName))
		{
			return ExtractEntitiesFromFile(fileName, dbContextContent);
		}

		// Without a real path the source goes through a temporary file
		var tempPath = Path.GetTempFileName();
		try
		{
			File.WriteAllText(tempPath, dbContextContent);
			return ExtractEntitiesFromFile(tempPath, dbContextContent);
		}
		finally
		{
			if (File.Exists(tempPath))
			{
				File.Delete(tempPath);
			}
		}
	}
}
