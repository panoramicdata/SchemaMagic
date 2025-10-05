namespace SchemaMagic.Tool;

public class EntityInfo
{
	public string Type { get; set; } = "";
	public string TableName { get; set; } = "";
	public List<PropertyInfo> Properties { get; set; } = [];
	public string? BaseType { get; set; }
	public List<PropertyInfo> InheritedProperties { get; set; } = [];
}
