using SchemaMagic.Core;
using System.Text.Json;

namespace SchemaMagic.Web.Services;

public class SchemaAnalysisService
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public Task<SchemaAnalysisResult> AnalyzeDbContextAsync(string dbContextContent, string fileName) =>
		AnalyzeDbContextAsync(dbContextContent, fileName, null);

	public async Task<SchemaAnalysisResult> AnalyzeDbContextAsync(string dbContextContent, string fileName, string? documentGuid)
	{
		return await Task.Run(() =>
		{
			var result = CoreSchemaAnalysisService.AnalyzeDbContextContent(dbContextContent, fileName);

			// A caller-supplied GUID keeps saved layout state attached to the document
			if (!string.IsNullOrEmpty(documentGuid) && result.Success)
			{
				var entitiesJson = JsonSerializer.Serialize(result.Entities, _jsonSerializerOptions);
				result.HtmlContent = ModularHtmlTemplate.Generate(entitiesJson, documentGuid);
				result.DocumentGuid = documentGuid;
			}

			return result;
		});
	}
}
