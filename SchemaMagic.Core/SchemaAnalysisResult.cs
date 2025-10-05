namespace SchemaMagic.Core;

public class SchemaAnalysisResult
{
	public bool Success { get; set; }
	public string? ErrorMessage { get; set; }
	public string HtmlContent { get; set; } = string.Empty;
	public int EntitiesFound { get; set; }
	public string DocumentGuid { get; set; } = string.Empty;
	public Dictionary<string, EntityInfo> Entities { get; set; } = [];
}