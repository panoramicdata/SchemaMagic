namespace SchemaMagic.Core;

/// <summary>
/// Contains the output of a schema analysis operation.
/// </summary>
public class SchemaAnalysisResult
{
	/// <summary>Gets or sets a value indicating whether analysis succeeded.</summary>
	public bool Success { get; set; }

	/// <summary>Gets or sets the error message when analysis fails.</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>Gets or sets the generated HTML document.</summary>
	public string HtmlContent { get; set; } = string.Empty;

	/// <summary>Gets or sets the number of entities discovered.</summary>
	public int EntitiesFound { get; set; }

	/// <summary>Gets or sets the identifier used to scope the generated document's state.</summary>
	public string DocumentGuid { get; set; } = string.Empty;

	/// <summary>Gets or sets the discovered entities, keyed by type name.</summary>
	public Dictionary<string, EntityInfo> Entities { get; set; } = [];
}
