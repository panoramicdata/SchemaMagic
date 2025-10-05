using SchemaMagic.Core;

namespace SchemaMagic.Web.Services;

public class SchemaAnalysisService
{
	public async Task<SchemaAnalysisResult> AnalyzeDbContextAsync(string dbContextContent)
	{
		return await Task.Run(() => CoreSchemaAnalysisService.AnalyzeDbContextContent(dbContextContent));
	}
}