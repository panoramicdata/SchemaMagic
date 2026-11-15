using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SchemaMagic.Core;

public static partial class CoreSchemaAnalysisService
{
	private static readonly HashSet<string> _systemBaseTypes =
	[
		"object", "Object", "System.Object",
		"ValueType", "System.ValueType",
		"Enum", "System.Enum",
		"Delegate", "System.Delegate",
		"MulticastDelegate", "System.MulticastDelegate"
	];

	private static void PopulateEntities(
		Dictionary<string, EntityInfo> entities,
		List<ClassDeclarationSyntax> allClasses,
		Dictionary<string, List<string>> foreignKeyRelationships)
	{
		foreach (var (entityName, entity) in entities)
		{
			var entityClass = allClasses.FirstOrDefault(c => c.Identifier.Text == entityName);
			if (entityClass is null)
			{
				entity.Properties = CreateFallbackProperties(entityName, foreignKeyRelationships);
				Console.WriteLine($"   ❌ Could not find entity class for {entityName}; created fallback with {entity.Properties.Count} properties");
				continue;
			}

			PopulateFromClass(entity, entityClass, allClasses, foreignKeyRelationships);
		}
	}

	private static void PopulateFromClass(
		EntityInfo entity,
		ClassDeclarationSyntax entityClass,
		List<ClassDeclarationSyntax> allClasses,
		Dictionary<string, List<string>> foreignKeyRelationships)
	{
		var entityName = entity.Type;
		var entityComment = ExtractComment(entityClass);
		if (!string.IsNullOrEmpty(entityComment))
		{
			entity.Comment = entityComment;
		}

		entity.Properties = ExtractProperties(entityClass, entityName, foreignKeyRelationships);

		var baseType = entityClass.BaseList?.Types.FirstOrDefault()?.ToString();
		if (baseType is null || baseType.Contains("DbContext") || _systemBaseTypes.Contains(baseType))
		{
			return;
		}

		entity.BaseType = baseType;
		var baseClass = allClasses.FirstOrDefault(c => c.Identifier.Text == baseType);
		if (baseClass is null)
		{
			Console.WriteLine($"   ⚠️ Base class {baseType} not found in discovered classes");
			return;
		}

		entity.InheritedProperties = ExtractProperties(baseClass, baseType, foreignKeyRelationships);
	}

	// Id, any foreign keys known from the snapshot, and a Name column when none is known
	private static List<PropertyInfo> CreateFallbackProperties(
		string entityName,
		Dictionary<string, List<string>> foreignKeyRelationships)
	{
		var properties = new List<PropertyInfo>
		{
			new() { Name = "Id", Type = "int", IsKey = true, IsForeignKey = false }
		};

		if (foreignKeyRelationships.TryGetValue(entityName, out var foreignKeys))
		{
			properties.AddRange(foreignKeys
				.Where(name => name != "Id")
				.Select(name => new PropertyInfo { Name = name, Type = "int", IsKey = false, IsForeignKey = true }));
		}

		if (!properties.Any(p => p.Name.Contains("Name")))
		{
			properties.Add(new PropertyInfo { Name = "Name", Type = "string", IsKey = false, IsForeignKey = false });
		}

		return properties;
	}
}
