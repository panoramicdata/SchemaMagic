namespace SchemaMagic.Core;

/// <summary>
/// Describes an entity discovered in a DbContext.
/// </summary>
public class EntityInfo
{
	/// <summary>Gets or sets the entity type name.</summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>Gets or sets the entity's base type name, if any.</summary>
	public string BaseType { get; set; } = string.Empty;

	/// <summary>Gets or sets the properties declared by the entity.</summary>
	public List<PropertyInfo> Properties { get; set; } = [];

	/// <summary>Gets or sets the properties inherited from the entity's base type.</summary>
	public List<PropertyInfo> InheritedProperties { get; set; } = [];

	/// <summary>Gets or sets the entity documentation comment.</summary>
	public string? Comment { get; set; }
}
