namespace SchemaMagic.Core;

public class PropertyInfo
{
	public string Name { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public bool IsKey { get; set; }
	public bool IsForeignKey { get; set; }
	public string? Comment { get; set; }

	/// <summary>
	/// Maximum length of the column, when constrained by a [MaxLength]/[StringLength] attribute
	/// (or [MaxLength]-equivalent). Null when the column is unbounded.
	/// </summary>
	public int? MaxLength { get; set; }
}
