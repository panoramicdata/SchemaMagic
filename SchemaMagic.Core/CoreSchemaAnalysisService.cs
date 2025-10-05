using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SchemaMagic.Core;

public static partial class CoreSchemaAnalysisService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static SchemaAnalysisResult AnalyzeDbContextContent(string dbContextContent, string fileName)
    {
        try
        {
            // Create a temporary file to preserve the original sophisticated analysis logic
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, dbContextContent);

            var entities = ExtractEntitiesFromFile(tempFile, dbContextContent);

            // Clean up temp file
            File.Delete(tempFile);

            var entitiesJson = JsonSerializer.Serialize(entities, _jsonSerializerOptions);
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

    private static Dictionary<string, EntityInfo> ExtractEntitiesFromFile(string tempFilePath, string sourceCode)
    {
        var entities = new Dictionary<string, EntityInfo>();

        // Parse the source code
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetCompilationUnitRoot();

        // Try to find the EF model snapshot first for accurate FK relationships
        var contextDirectory = Path.GetDirectoryName(tempFilePath);
        var migrationDirectory = Path.Combine(contextDirectory ?? "", "Migrations");
        var snapshotPath = Directory.Exists(migrationDirectory)
            ? Directory.GetFiles(migrationDirectory, "*ModelSnapshot.cs").FirstOrDefault()
            : null;

        var foreignKeyRelationships = new Dictionary<string, List<string>>();

        if (snapshotPath != null && File.Exists(snapshotPath))
        {
            Console.WriteLine($"?? Found EF Snapshot: {Path.GetFileName(snapshotPath)}");
            foreignKeyRelationships = ExtractForeignKeysFromSnapshot(snapshotPath);
            Console.WriteLine($"?? Extracted {foreignKeyRelationships.Sum(kvp => kvp.Value.Count)} foreign key relationships");
        }
        else
        {
            Console.WriteLine("?? No EF Model Snapshot found - using heuristic FK detection");
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

        Console.WriteLine($"?? Found {dbSetProperties.Count} DbSet properties");

        foreach (var dbSetProperty in dbSetProperties)
        {
            var entityTypeName = ExtractEntityTypeName(dbSetProperty.Type.ToString());
            if (entityTypeName != null)
            {
                Console.WriteLine($"?? Discovered entity: {entityTypeName}");
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
        Console.WriteLine($"?? Found {allClasses.Count} classes in current file");

        // Strategy 2: Look in Models subdirectory (if temp file is in a real directory structure)
        var modelsDirectory = Path.Combine(contextDirectory ?? "", "Models");
        var allModelClasses = new List<ClassDeclarationSyntax>();

        if (Directory.Exists(modelsDirectory))
        {
            Console.WriteLine($"?? Looking for entity models in: {modelsDirectory}");
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
                    Console.WriteLine($"?? Warning: Could not parse model file {Path.GetFileName(modelFile)}: {ex.Message}");
                }
            }

            Console.WriteLine($"?? Found {allModelClasses.Count} model classes in Models directory");
        }

        // Strategy 3: Enhanced search in related projects (simplified for Core library)
        var solutionDirectory = FindSolutionDirectory(contextDirectory);
        var additionalEntityClasses = new List<ClassDeclarationSyntax>();

        if (solutionDirectory != null)
        {
            Console.WriteLine($"?? Solution directory: {solutionDirectory}");

            // Target specific data projects by name
            var targetDataProjects = new[]
            {
                "MagicSuite.Data",
                "AlertMagic.Core",
                "DataMagic.EfCore",
                "ConnectMagic",
                "DocMagic.Data"
            };

            foreach (var projectName in targetDataProjects)
            {
                var projectPath = Path.Combine(solutionDirectory, projectName);
                if (Directory.Exists(projectPath))
                {
                    Console.WriteLine($"?? Searching targeted project: {projectName}");
                    var foundCount = SearchProjectForEntities(projectPath, entities.Keys, additionalEntityClasses);
                    if (foundCount > 0)
                    {
                        Console.WriteLine($"   ?? Found {foundCount} entities in {projectName}");
                    }
                }
            }
        }

        // Combine all discovered classes
        allClasses.AddRange(allModelClasses);
        allClasses.AddRange(additionalEntityClasses);
        Console.WriteLine($"?? Total classes found: {allClasses.Count}");
        Console.WriteLine($"?? Unique class names: {allClasses.Select(c => c.Identifier.Text).Distinct().Count()}");

        // Now extract properties for each entity
        foreach (var entityName in entities.Keys.ToList())
        {
            Console.WriteLine($"?? Processing entity: {entityName}");

            // Check if we have FK data for this entity
            if (foreignKeyRelationships.TryGetValue(entityName, out List<string>? value))
            {
                Console.WriteLine($"   ? Found FK data: {string.Join(", ", value)}");
            }

            var entityClass = allClasses.FirstOrDefault(c => c.Identifier.Text == entityName);
            if (entityClass != null)
            {
                Console.WriteLine($"   ? Found entity class: {entityName}");
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
                            Console.WriteLine($"   ?? Found base class: {baseType}");
                            var inheritedProperties = ExtractProperties(baseClass, baseType, foreignKeyRelationships);
                            entities[entityName].InheritedProperties = inheritedProperties;
                        }
                        else
                        {
                            Console.WriteLine($"   ?? Base class {baseType} not found in discovered classes");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"   ? Could not find entity class for {entityName}");

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
                if (!properties.Any(p => p.Name.Contains("Name")))
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
                Console.WriteLine($"   ?? Created enhanced fallback entity with {properties.Count} properties");
            }
        }

        Console.WriteLine($"? Entity discovery complete. Found {entities.Count} entities with properties.");
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
                                Console.WriteLine($"   ? Found {classDecl.Identifier.Text} in {Path.GetRelativePath(projectPath, csFile)}");
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
            Console.WriteLine($"   ?? Error searching project: {ex.Message}");
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
                            Console.WriteLine($"??? Processing EF entity: {currentEntity}");
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

            Console.WriteLine($"?? EF Snapshot Summary:");
            Console.WriteLine($"   Total entities with FKs: {foreignKeys.Count}");
            foreach (var kvp in foreignKeys)
            {
                Console.WriteLine($"   {kvp.Key}: {string.Join(", ", kvp.Value)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"?? Warning: Could not parse EF snapshot: {ex.Message}");
        }

        return foreignKeys;
    }

    private static List<PropertyInfo> ExtractProperties(ClassDeclarationSyntax entityClass, string entityName, Dictionary<string, List<string>> foreignKeyRelationships)
    {
        var properties = new List<PropertyInfo>();
        var entityForeignKeys = foreignKeyRelationships.GetValueOrDefault(entityName, []);

        Console.WriteLine($"?? Extracting properties for {entityName}, FK count: {entityForeignKeys.Count}");
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

            // Check if this property is a foreign key (from EF snapshot OR heuristic detection)
            var isForeignKey = entityForeignKeys.Contains(propertyName) || IsHeuristicForeignKey(propertyName, propertyType);

            var propertyInfo = new PropertyInfo
            {
                Name = propertyName,
                Type = propertyType,
                IsKey = IsKeyProperty(propertyName, entityName),
                IsForeignKey = isForeignKey
            };

            Console.WriteLine($"   Property: {propertyName} ({propertyType}) - Key: {propertyInfo.IsKey}, FK: {propertyInfo.IsForeignKey}");
            properties.Add(propertyInfo);
        }

        Console.WriteLine($"? Extracted {properties.Count} properties for {entityName}");
        return properties;
    }

    private static bool IsHeuristicForeignKey(string propertyName, string propertyType)
    {
        // Heuristic FK detection for when no EF snapshot is available
        return propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
               !propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) && // Not the primary key "Id"
               (propertyType.Contains("int") || propertyType.Contains("Guid")); // FK types
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
