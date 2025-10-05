using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaMagic.Tool;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SchemaMagic;

public partial class SchemaGenerator(FileInfo dbContextFile, string? outputPath = null, string? documentGuid = null, string? customCssPath = null)
{
	private readonly string _dbContextPath = dbContextFile.FullName;
	private readonly string? _documentGuid = documentGuid;
	private readonly string? _customCssPath = customCssPath;
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public Task<string> GenerateSchemaVisualizationAsync() => Task.FromResult(GenerateSchemaVisualization());

	// Static async methods for Program.cs
	public static async Task ExportDefaultCssAsync(string cssOutputPath)
	{
		await Task.Run(() => OutputDefaultCss(cssOutputPath));
	}

	public static async Task<string> GenerateSchemaAsync(FileInfo dbContextFile, string? output, string? guid, FileInfo? cssFile)
	{
		var cssPath = cssFile?.FullName;
		var generator = new SchemaGenerator(dbContextFile, output, guid, cssPath);
		return await generator.GenerateSchemaVisualizationAsync();
	}

	public static void OutputDefaultCss(string outputPath)
	{
		Console.WriteLine("🎨 Exporting default CSS stylesheet...");

		// Ensure output directory exists
		var outputDirectory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}

		// Get the default CSS content
		var defaultCss = ModularHtmlTemplate.GetDefaultCss();

		// Write to file
		File.WriteAllText(outputPath, defaultCss);

		Console.WriteLine($"📄 Default CSS exported successfully");
		Console.WriteLine($"📂 Location: {Path.GetFullPath(outputPath)}");
		Console.WriteLine($"📏 Size: {new FileInfo(outputPath).Length:N0} bytes");
		Console.WriteLine();
		Console.WriteLine("💡 Usage Tips:");
		Console.WriteLine("   • Modify colors, fonts, sizes, and layout in this file");
		Console.WriteLine("   • Use --css-file path/to/your-styles.css to apply customizations");
		Console.WriteLine("   • Your changes will override the default styling");
		Console.WriteLine("   • Invalid CSS will be ignored, falling back to defaults");
	}

	public string GenerateSchemaVisualization()
	{
		if (!File.Exists(_dbContextPath))
		{
			throw new FileNotFoundException($"DbContext file not found: {_dbContextPath}");
		}

		// Validate custom CSS file if provided
		if (!string.IsNullOrEmpty(_customCssPath))
		{
			if (!File.Exists(_customCssPath))
			{
				throw new FileNotFoundException($"Custom CSS file not found: {_customCssPath}");
			}

			Console.WriteLine($"🎨 Using custom CSS: {Path.GetFileName(_customCssPath)}");
		}

		Console.WriteLine("🚀 SchemaMagic - Interactive Database Schema Visualizer");
		Console.WriteLine("============================================================");
		Console.WriteLine($"🔍 Processing DbContext: {Path.GetFileName(_dbContextPath)}");

		// Read and parse the DbContext file
		var sourceCode = File.ReadAllText(_dbContextPath);
		var tree = CSharpSyntaxTree.ParseText(sourceCode);
		var root = tree.GetCompilationUnitRoot();

		// Extract entities and their properties
		var entities = ExtractEntities(root, _dbContextPath);

		Console.WriteLine($"📊 Found {entities.Count} entities");

		if (entities.Count == 0)
		{
			Console.WriteLine("❌ No entities found in the DbContext file.");
			throw new InvalidOperationException("No entities found in the DbContext file.");
		}

		// Generate HTML content with GUID and CSS support
		var entitiesJson = JsonSerializer.Serialize(entities, _jsonSerializerOptions);

		// Generate GUID - use provided GUID or create new one
		// NOTE: Using Guid.Empty (00000000-0000-0000-0000-000000000000) is useful
		// for testing download functionality while preserving localStorage state
		// from a previous document with the same GUID
		var documentGuid = _documentGuid ?? Guid.NewGuid().ToString();
		var htmlContent = ModularHtmlTemplate.Generate(entitiesJson, documentGuid, _customCssPath);

		// Determine output path
		var contextDirectory = Path.GetDirectoryName(_dbContextPath) ?? Directory.GetCurrentDirectory();
		var outputDirectory = Path.Combine(contextDirectory, "Output");
		Directory.CreateDirectory(outputDirectory);

		var fileName = outputPath ?? $"{Path.GetFileNameWithoutExtension(_dbContextPath)}-Schema.html";
		if (!fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
		{
			fileName += ".html";
		}

		var fullOutputPath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(outputDirectory, Path.GetFileName(fileName));

		// Write the HTML file
		File.WriteAllText(fullOutputPath, htmlContent);

		Console.WriteLine("✅ Interactive schema visualization generated!");
		Console.WriteLine($"📄 Output file: {fullOutputPath}");
		Console.WriteLine($"🆔 Document GUID: {documentGuid}");
		if (!string.IsNullOrEmpty(_documentGuid))
		{
			Console.WriteLine("🔄 Using provided GUID - will preserve existing localStorage state");
		}
		else
		{
			Console.WriteLine("🆕 New document - will auto-optimize layout on first load");
		}

		if (!string.IsNullOrEmpty(_customCssPath))
		{
			Console.WriteLine($"🎨 Custom styling applied from: {Path.GetFileName(_customCssPath)}");
		}

		Console.WriteLine("📊 Features: Drag tables, Pan background, Zoom controls, Toggle relations");

		return fullOutputPath;
	}

	private static Dictionary<string, EntityInfo> ExtractEntities(CompilationUnitSyntax root, string dbContextPath)
	{
		var entities = new Dictionary<string, EntityInfo>();

		// Try to find the EF model snapshot first for accurate FK relationships
		var contextDirectory = Path.GetDirectoryName(dbContextPath);
		var migrationDirectory = Path.Combine(contextDirectory ?? "", "Migrations");
		var snapshotPath = Directory.Exists(migrationDirectory)
			? Directory.GetFiles(migrationDirectory, "*ModelSnapshot.cs").FirstOrDefault()
			: null;

		var foreignKeyRelationships = new Dictionary<string, List<string>>();

		if (snapshotPath != null && File.Exists(snapshotPath))
		{
			Console.WriteLine($"📋 Found EF Snapshot: {Path.GetFileName(snapshotPath)}");
			foreignKeyRelationships = ExtractForeignKeysFromSnapshot(snapshotPath);
			Console.WriteLine($"🔗 Extracted {foreignKeyRelationships.Sum(kvp => kvp.Value.Count)} foreign key relationships");
		}
		else
		{
			Console.WriteLine("⚠️ No EF Model Snapshot found - using heuristic FK detection");
		}

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

		Console.WriteLine($"🔍 Found {dbSetProperties.Count} DbSet properties");

		foreach (var dbSetProperty in dbSetProperties)
		{
			var entityTypeName = ExtractEntityTypeName(dbSetProperty.Type.ToString());
			if (entityTypeName != null)
			{
				Console.WriteLine($"📝 Discovered entity: {entityTypeName}");
				var entityInfo = new EntityInfo
				{
					Type = entityTypeName,
					Properties = []
				};

				entities[entityTypeName] = entityInfo;
			}
		}

		// Strategy 1: Look in the current file
		var allClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
		Console.WriteLine($"📁 Found {allClasses.Count} classes in current file");

		// Strategy 2: Look in Models subdirectory
		var modelsDirectory = Path.Combine(contextDirectory ?? "", "Models");
		var allModelClasses = new List<ClassDeclarationSyntax>();

		if (Directory.Exists(modelsDirectory))
		{
			Console.WriteLine($"📁 Looking for entity models in: {modelsDirectory}");
			var modelFiles = Directory.GetFiles(modelsDirectory, "*.cs");

			foreach (var modelFile in modelFiles)
			{
				try
				{
					var modelSourceCode = File.ReadAllText(modelFile);
					var modelTree = CSharpSyntaxTree.ParseText(modelSourceCode);
					var modelRoot = modelTree.GetCompilationUnitRoot();
					var modelClasses = modelRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
					allModelClasses.AddRange(modelClasses);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"⚠️ Warning: Could not parse model file {Path.GetFileName(modelFile)}: {ex.Message}");
				}
			}

			Console.WriteLine($"📊 Found {allModelClasses.Count} model classes in Models directory");
		}

		// Strategy 3: Enhanced search in related projects
		var solutionDirectory = FindSolutionDirectory(contextDirectory);
		var additionalEntityClasses = new List<ClassDeclarationSyntax>();

		if (solutionDirectory != null)
		{
			Console.WriteLine($"🔍 Solution directory: {solutionDirectory}");

			// Target specific data projects by name
			var targetDataProjects = new[]
			{
				"MagicSuite.Data",
				"AlertMagic.Core", // Since we see AlertMagic entities
				"DataMagic.EfCore", // Since we see DataMagic entities
				"ConnectMagic", // Since we see Connect entities
				"DocMagic.Data"
			};

			foreach (var projectName in targetDataProjects)
			{
				var projectPath = Path.Combine(solutionDirectory, projectName);
				if (Directory.Exists(projectPath))
				{
					Console.WriteLine($"🎯 Searching targeted project: {projectName}");
					var foundCount = SearchProjectForEntities(projectPath, entities.Keys, additionalEntityClasses);
					if (foundCount > 0)
					{
						Console.WriteLine($"   📊 Found {foundCount} entities in {projectName}");
					}
				}
			}

			// Also do a broader search for data projects
			var dataProjectPaths = new List<string>();

			// Common data project patterns
			var dataProjectPatterns = new[]
			{
				"*.Data",
				"*.Data.Models",
				"*.Models"
			};

			foreach (var pattern in dataProjectPatterns)
			{
				try
				{
					var matchingDirs = Directory.GetDirectories(solutionDirectory, pattern, SearchOption.TopDirectoryOnly);
					foreach (var dir in matchingDirs)
					{
						var dirName = Path.GetFileName(dir);
						if (!targetDataProjects.Contains(dirName)) // Don't duplicate targeted projects
						{
							dataProjectPaths.Add(dir);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"⚠️ Warning searching for pattern {pattern}: {ex.Message}");
				}
			}

			Console.WriteLine($"📁 Found {dataProjectPaths.Count} additional data project directories");

			foreach (var dataProjectPath in dataProjectPaths.Take(3)) // Limit to avoid too many searches
			{
				try
				{
					var projectName = Path.GetFileName(dataProjectPath);
					Console.WriteLine($"🔍 Searching additional project: {projectName}");

					var foundCount = SearchProjectForEntities(dataProjectPath, entities.Keys, additionalEntityClasses);
					if (foundCount > 0)
					{
						Console.WriteLine($"   📊 Found {foundCount} entities in {projectName}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"⚠️ Warning: Could not search project {Path.GetFileName(dataProjectPath)}: {ex.Message}");
				}
			}
		}

		// Combine all discovered classes
		allClasses.AddRange(allModelClasses);
		allClasses.AddRange(additionalEntityClasses);
		Console.WriteLine($"📊 Total classes found: {allClasses.Count}");
		Console.WriteLine($"📊 Unique class names: {allClasses.Select(c => c.Identifier.Text).Distinct().Count()}");

		// Now extract properties for each entity
		foreach (var entityName in entities.Keys.ToList())
		{
			Console.WriteLine($"🔍 Processing entity: {entityName}");

			// Check if we have FK data for this entity
			if (foreignKeyRelationships.TryGetValue(entityName, out List<string>? value))
			{
				Console.WriteLine($"   ✅ Found FK data: {string.Join(", ", value)}");
			}

			var entityClass = allClasses.FirstOrDefault(c => c.Identifier.Text == entityName);
			if (entityClass != null)
			{
				Console.WriteLine($"   ✅ Found entity class: {entityName}");
				var properties = ExtractProperties(entityClass, entityName, foreignKeyRelationships);
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
							Console.WriteLine($"   📄 Found base class: {baseType}");
							var inheritedProperties = ExtractProperties(baseClass, baseType, foreignKeyRelationships);
							entities[entityName].InheritedProperties = inheritedProperties;
						}
						else
						{
							Console.WriteLine($"   ⚠️ Base class {baseType} not found in discovered classes");
						}
					}
				}
			}
			else
			{
				Console.WriteLine($"   ❌ Could not find entity class for {entityName}");

				// Try to create a more complete fallback entity based on FK information
				var properties = new List<PropertyInfo>
				{
					new()
					{
						Name = "Id",
						Type = "int",
						IsKey = true,
						IsForeignKey = false
					}
				};

				// Add FK properties we know about
				if (foreignKeyRelationships.TryGetValue(entityName, out var fkProperties))
				{
					foreach (var fkProp in fkProperties)
					{
						if (fkProp != "Id") // Don't duplicate the Id property
						{
							properties.Add(new PropertyInfo
							{
								Name = fkProp,
								Type = "int",
								IsKey = false,
								IsForeignKey = true
							});
						}
					}
				}

				// Add common properties for entities
				if (!properties.Any(p => p.Name.Contains("Name")) && entityName != "AgeingEvent") // AgeingEvent inherits from NamedItem
				{
					properties.Add(new PropertyInfo
					{
						Name = "Name",
						Type = "string",
						IsKey = false,
						IsForeignKey = false
					});
				}

				entities[entityName].Properties = properties;
				Console.WriteLine($"   🔧 Created enhanced fallback entity with {properties.Count} properties");
			}
		}

		Console.WriteLine($"✅ Entity discovery complete. Found {entities.Count} entities with properties.");
		return entities;
	}

	private static int SearchProjectForEntities(string projectPath, IEnumerable<string> targetEntityNames, List<ClassDeclarationSyntax> foundClasses)
	{
		var foundCount = 0;
		var targetSet = new HashSet<string>(targetEntityNames);

		try
		{
			// Search for CS files recursively but limit depth to avoid performance issues
			var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
				.Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\")) // Skip build artifacts
				.Take(200); // Reasonable limit

			foreach (var csFile in csFiles)
			{
				try
				{
					var fileName = Path.GetFileNameWithoutExtension(csFile);

					// Only process files that might be our entities
					if (targetSet.Contains(fileName))
					{
						var sourceCode = File.ReadAllText(csFile);
						var tree = CSharpSyntaxTree.ParseText(sourceCode);
						var rootNode = tree.GetCompilationUnitRoot();
						var classes = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

						foreach (var classDecl in classes)
						{
							if (targetSet.Contains(classDecl.Identifier.Text))
							{
								foundClasses.Add(classDecl);
								foundCount++;
								Console.WriteLine($"   ✅ Found {classDecl.Identifier.Text} in {Path.GetRelativePath(projectPath, csFile)}");
							}
						}
					}
				}
				catch (Exception)
				{
					// Silently skip files that can't be parsed
					continue;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"   ⚠️ Error searching project: {ex.Message}");
		}

		return foundCount;
	}

	private static string? FindSolutionDirectory(string? startDirectory)
	{
		if (string.IsNullOrEmpty(startDirectory))
			return null;

		var currentDirectory = new DirectoryInfo(startDirectory);

		while (currentDirectory != null)
		{
			// Look for .sln files
			if (currentDirectory.GetFiles("*.sln").Length > 0)
			{
				return currentDirectory.FullName;
			}

			currentDirectory = currentDirectory.Parent;
		}

		return null;
	}

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

	private static Dictionary<string, List<string>> ExtractForeignKeysFromSnapshot(string snapshotPath)
	{
		var foreignKeys = new Dictionary<string, List<string>>();

		try
		{
			var snapshotContent = File.ReadAllText(snapshotPath);
			var tree = CSharpSyntaxTree.ParseText(snapshotContent);
			var root = tree.GetCompilationUnitRoot();

			// Find the BuildModel method
			var buildModelMethod = root.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault(m => m.Identifier.Text == "BuildModel");

			if (buildModelMethod != null)
			{
				// Look for HasOne relationship configurations using string matching
				var lines = snapshotContent.Split('\n');
				string? currentEntity = null;

				for (int i = 0; i < lines.Length; i++)
				{
					var line = lines[i].Trim();

					// Detect when we're starting a new entity relationship configuration
					if (line.Contains("modelBuilder.Entity(") && line.Contains(", b =>"))
					{
						// Extract entity name from modelBuilder.Entity("DataMagic.EfCore.Models.EntityNameModel", b =>
						var match = Regex.Match(line, @"Entity\(""[^""]*\.([^""\.]+)""");
						if (match.Success)
						{
							currentEntity = match.Groups[1].Value;
							Console.WriteLine($"🏗️ Processing EF entity: {currentEntity}");
						}
					}

					// Look for HasForeignKey within the relationship chain
					if (currentEntity != null && line.Contains(".HasForeignKey(") && line.Contains('\"'))
					{
						// Extract foreign key property name
						var fkMatch = HasForeignKeyRegex().Match(line);
						if (fkMatch.Success)
						{
							var foreignKeyProperty = fkMatch.Groups[1].Value;

							if (!foreignKeys.TryGetValue(currentEntity, out var value))
							{
								value = [];
								foreignKeys[currentEntity] = value;
							}

							value.Add(foreignKeyProperty);

							Console.WriteLine($"FK: {currentEntity}.{foreignKeyProperty}");
						}
					}
				}
			}

			Console.WriteLine($"📋 EF Snapshot Summary:");
			Console.WriteLine($"   Total entities with FKs: {foreignKeys.Count}");
			foreach (var kvp in foreignKeys)
			{
				Console.WriteLine($"   {kvp.Key}: {string.Join(", ", kvp.Value)}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️ Warning: Could not parse EF snapshot: {ex.Message}");
		}

		return foreignKeys;
	}

	private static List<PropertyInfo> ExtractProperties(ClassDeclarationSyntax entityClass, string entityName, Dictionary<string, List<string>> foreignKeyRelationships)
	{
		var properties = new List<PropertyInfo>();
		var entityForeignKeys = foreignKeyRelationships.GetValueOrDefault(entityName, []);

		Console.WriteLine($"🔍 Extracting properties for {entityName}, FK count: {entityForeignKeys.Count}");
		if (entityForeignKeys.Count > 0)
		{
			Console.WriteLine($"   FK properties: {string.Join(", ", entityForeignKeys)}");
		}

		var propertyDeclarations = entityClass.Members
			.OfType<PropertyDeclarationSyntax>()
			.Where(p => p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true);

		foreach (var property in propertyDeclarations)
		{
			var propertyName = property.Identifier.Text;
			var propertyType = property.Type.ToString();

			// Clean up the type string
			propertyType = propertyType.Replace("?", "").Trim();

			// Check if this property is a foreign key
			var isForeignKey = entityForeignKeys.Contains(propertyName);

			var propertyInfo = new PropertyInfo
			{
				Name = propertyName,
				Type = propertyType,
				IsKey = IsKeyProperty(propertyName, entityName),
				IsForeignKey = isForeignKey // Use the FK data from EF snapshot
			};

			Console.WriteLine($"   Property: {propertyName} ({propertyType}) - Key: {propertyInfo.IsKey}, FK: {propertyInfo.IsForeignKey}");
			properties.Add(propertyInfo);
		}

		Console.WriteLine($"✅ Extracted {properties.Count} properties for {entityName}");
		return properties;
	}

	private static bool IsKeyProperty(string propertyName, string entityName) =>
		// Standard EF key conventions
		propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
			   propertyName.Equals($"{entityName}Id", StringComparison.OrdinalIgnoreCase);

	private static string? ExtractEntityTypeName(string dbSetType)
	{
		// Extract entity type from DbSet<EntityType>
		var match = Regex.Match(dbSetType, @"DbSet<(.+?)>");
		return match.Success ? match.Groups[1].Value : null;
	}

	[GeneratedRegex(@"\.HasForeignKey\(""([^""]+)""\)")]
	private static partial Regex HasForeignKeyRegex();
}
