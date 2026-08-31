namespace SchemaMagic.Core;

/// <summary>
/// Describes a property discovered on an entity.
/// </summary>
public class PropertyInfo
{
	/// <summary>Gets or sets the property name.</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>Gets or sets the C# property type.</summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>Gets or sets a value indicating whether the property is a primary key.</summary>
	public bool IsKey { get; set; }

	/// <summary>Gets or sets a value indicating whether the property is a foreign key.</summary>
	public bool IsForeignKey { get; set; }

	/// <summary>Gets or sets the property documentation comment.</summary>
	public string? Comment { get; set; }

	/// <summary>
	/// Maximum length of the column, when constrained by a [MaxLength]/[StringLength] attribute
	/// (or [MaxLength]-equivalent). Null when the column is unbounded.
	/// </summary>
	public int? MaxLength { get; set; }
}
