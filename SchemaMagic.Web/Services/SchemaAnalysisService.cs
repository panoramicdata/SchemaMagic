using SchemaMagic.Core;

namespace SchemaMagic.Web.Services;

public class SchemaAnalysisService
{
    public async Task<SchemaAnalysisResult> AnalyzeDbContextAsync(string dbContextContent, string fileName, string? documentGuid = null)
    {
        return await Task.Run(() => 
        {
            var result = CoreSchemaAnalysisService.AnalyzeDbContextContent(dbContextContent, fileName);
            
            // If a specific GUID was provided and analysis was successful, regenerate HTML with that GUID
            if (!string.IsNullOrEmpty(documentGuid) && result.Success)
            {
                var entitiesJson = System.Text.Json.JsonSerializer.Serialize(result.Entities, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                
                result.HtmlContent = ModularHtmlTemplate.Generate(entitiesJson, documentGuid, null);
                result.DocumentGuid = documentGuid;
            }
            
            return result;
        });
    }
}