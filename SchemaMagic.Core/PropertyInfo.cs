namespace SchemaMagic.Core;

public class PropertyInfo
{
	public string Name { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public bool IsKey { get; set; }
	public bool IsForeignKey { get; set; }
	public string? Comment { get; set; }
}
