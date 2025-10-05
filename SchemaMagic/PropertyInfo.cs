namespace SchemaMagic.Tool;

public class PropertyInfo
{
	public string Name { get; set; } = "";
	public string Type { get; set; } = "";
	public bool IsKey { get; set; }
	public bool IsForeignKey { get; set; }
}
