namespace SchemaMagic.Web.Services;

public class SchemaAnalysisService
{
	public async Task<SchemaAnalysisResult> AnalyzeDbContextAsync(string dbContextContent, string fileName)
	{
		try
		{
			// Create a temporary file to use the existing SchemaGenerator logic
			var tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), fileName));
			await File.WriteAllTextAsync(tempFile.FullName, dbContextContent);

			var generator = new SchemaGenerator(tempFile);
			var entities = await AnalyzeEntitiesAsync(dbContextContent, tempFile.FullName);

			// Clean up temp file
			if (tempFile.Exists)
				tempFile.Delete();

			var entitiesJson = JsonSerializer.Serialize(entities, new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});

			var documentGuid = Guid.NewGuid().ToString();
			var htmlContent = ModularHtmlTemplate.Generate(entitiesJson, documentGuid, null);

			return new SchemaAnalysisResult
			{
				Success = true,
				HtmlContent = htmlContent,
				EntitiesFound = entities.Count,
				DocumentGuid = documentGuid,
				Entities = entities
			};
		}
		catch (Exception ex)
		{
			return new SchemaAnalysisResult
			{
				Success = false,
				ErrorMessage = ex.Message,
				EntitiesFound = 0
			};
		}
	}

	private async Task<Dictionary<string, EntityInfo>> AnalyzeEntitiesAsync(string sourceCode, string dbContextPath)
	{
		var entities = new Dictionary<string, EntityInfo>();

		try
		{
			var tree = CSharpSyntaxTree.ParseText(sourceCode);
			var root = tree.GetCompilationUnitRoot();

			// Find DbContext class
			var dbContextClass = root.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.FirstOrDefault(c => c.BaseList?.Types.Any(t => t.ToString().Contains("DbContext")) == true)
				?? throw new InvalidOperationException("No DbContext class found in the file.");

			// Extract DbSet properties to find entity types
			var dbSetProperties = dbContextClass.Members
				.OfType<PropertyDeclarationSyntax>()
				.Where(p => p.Type.ToString().StartsWith("DbSet<", StringComparison.OrdinalIgnoreCase))
				.ToList();

			foreach (var dbSetProperty in dbSetProperties)
			{
				var entityTypeName = ExtractEntityTypeName(dbSetProperty.Type.ToString());
				if (entityTypeName != null)
				{
					var entityInfo = new EntityInfo
					{
						Type = entityTypeName,
						Properties = []
					};

					entities[entityTypeName] = entityInfo;
				}
			}

			// Look for entity classes in the same file
			var allClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

			// Extract properties for each entity
			foreach (var entityName in entities.Keys.ToList())
			{
				var entityClass = allClasses.FirstOrDefault(c => c.Identifier.Text == entityName);
				if (entityClass != null)
				{
					var properties = ExtractProperties(entityClass, entityName);
					entities[entityName].Properties = properties;

					// Extract inheritance information
					if (entityClass.BaseList?.Types.Count > 0)
					{
						var baseType = entityClass.BaseList.Types.First().ToString();
						if (baseType != "object" && !baseType.Contains("DbContext") && !IsSystemBaseType(baseType))
						{
							entities[entityName].BaseType = baseType;

							// Extract inherited properties
							var baseClass = allClasses.FirstOrDefault(c => c.Identifier.Text == baseType);
							if (baseClass != null)
							{
								var inheritedProperties = ExtractProperties(baseClass, baseType);
								entities[entityName].InheritedProperties = inheritedProperties;
							}
						}
					}
				}
				else
				{
					// Create fallback entity
					var properties = new List<PropertyInfo>
					{
						new()
						{
							Name = "Id",
							Type = "int",
							IsKey = true,
							IsForeignKey = false
						},
						new()
						{
							Name = "Name",
							Type = "string",
							IsKey = false,
							IsForeignKey = false
						}
					};

					entities[entityName].Properties = properties;
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Error analyzing DbContext: {ex.Message}");
		}

		return entities;
	}

	private static List<PropertyInfo> ExtractProperties(ClassDeclarationSyntax entityClass, string entityName)
	{
		var properties = new List<PropertyInfo>();

		var propertyDeclarations = entityClass.Members
			.OfType<PropertyDeclarationSyntax>()
			.Where(p => p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true);

		foreach (var property in propertyDeclarations)
		{
			var propertyName = property.Identifier.Text;
			var propertyType = property.Type.ToString();

			// Clean up the type string
			propertyType = propertyType.Replace("?", "").Trim();

			var propertyInfo = new PropertyInfo
			{
				Name = propertyName,
				Type = propertyType,
				IsKey = IsKeyProperty(propertyName, entityName),
				IsForeignKey = IsForeignKeyProperty(propertyName, propertyType)
			};

			properties.Add(propertyInfo);
		}

		return properties;
	}

	private static bool IsKeyProperty(string propertyName, string entityName) =>
		propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
		propertyName.Equals($"{entityName}Id", StringComparison.OrdinalIgnoreCase);

	private static bool IsForeignKeyProperty(string propertyName, string propertyType) =>
		propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
		(propertyType.Contains("int") || propertyType.Contains("Guid"));

	private static bool IsSystemBaseType(string baseType)
	{
		var systemBaseTypes = new HashSet<string>
		{
			"object", "Object", "System.Object",
			"ValueType", "System.ValueType",
			"Enum", "System.Enum",
			"Delegate", "System.Delegate",
			"MulticastDelegate", "System.MulticastDelegate"
		};

		return systemBaseTypes.Contains(baseType);
	}

	private static string? ExtractEntityTypeName(string dbSetType)
	{
		var match = Regex.Match(dbSetType, @"DbSet<(.+?)>");
		return match.Success ? match.Groups[1].Value : null;
	}
}

public class SchemaAnalysisResult
{
	public bool Success { get; set; }
	public string? ErrorMessage { get; set; }
	public string HtmlContent { get; set; } = string.Empty;
	public int EntitiesFound { get; set; }
	public string DocumentGuid { get; set; } = string.Empty;
	public Dictionary<string, EntityInfo> Entities { get; set; } = new();
}