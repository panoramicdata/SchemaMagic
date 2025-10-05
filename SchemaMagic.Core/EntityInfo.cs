namespace SchemaMagic.Core;

public class EntityInfo
{
	public string Type { get; set; } = string.Empty;
	public string BaseType { get; set; } = string.Empty;
	public List<PropertyInfo> Properties { get; set; } = [];
	public List<PropertyInfo> InheritedProperties { get; set; } = [];
}
