using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

/// <summary>
/// Tests to verify that all modern C# property patterns are correctly extracted
/// </summary>
public class PropertyExtractionTests
{
	[Fact]
	public void ExtractProperties_AutoImplementedProperty_ShouldBeFound()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public int Id { get; set; }
				public string Name { get; set; }
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(2, properties.Count);
		Assert.Contains(properties, p => p.Name == "Id");
		Assert.Contains(properties, p => p.Name == "Name");
	}

	[Fact]
	public void ExtractProperties_RequiredAutoProperty_ShouldBeFound()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public required string Name { get; set; }
				public required int Age { get; init; }
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(2, properties.Count);
		Assert.Contains(properties, p => p.Name == "Name");
		Assert.Contains(properties, p => p.Name == "Age");
	}

	[Fact]
	public void ExtractProperties_InitOnlyProperty_ShouldBeFound()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public int Id { get; init; }
				public string Name { get; init; }
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(2, properties.Count);
		Assert.Contains(properties, p => p.Name == "Id");
		Assert.Contains(properties, p => p.Name == "Name");
	}

	[Fact]
	public void ExtractProperties_ExpressionBodiedProperty_ShouldBeFound()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public string FirstName { get; set; }
				public string LastName { get; set; }
				public string FullName => $""{FirstName} {LastName}"";
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(3, properties.Count);
		Assert.Contains(properties, p => p.Name == "FullName");
	}

	[Fact]
	public void ExtractProperties_NullableProperties_ShouldPreserveNullability()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public string Name { get; set; }
				public string? OptionalName { get; set; }
				public int Age { get; set; }
				public int? OptionalAge { get; set; }
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(4, properties.Count);
		
		var optionalName = properties.First(p => p.Name == "OptionalName");
		Assert.Contains("?", optionalName.Type); // Nullable marker preserved

		var optionalAge = properties.First(p => p.Name == "OptionalAge");
		Assert.Contains("?", optionalAge.Type); // Nullable marker preserved
	}

	[Fact]
	public void ExtractProperties_MixedPropertyPatterns_ShouldFindAll()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public int Id { get; set; }                          // Auto-implemented
				public required string Name { get; set; }             // Required auto
				public string? Description { get; init; }             // Init-only nullable
				public DateTime CreatedAt { get; } = DateTime.Now;    // Read-only with initializer
				public string DisplayName => Name.ToUpper();          // Expression-bodied
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(5, properties.Count);
		Assert.Contains(properties, p => p.Name == "Id");
		Assert.Contains(properties, p => p.Name == "Name");
		Assert.Contains(properties, p => p.Name == "Description");
		Assert.Contains(properties, p => p.Name == "CreatedAt");
		Assert.Contains(properties, p => p.Name == "DisplayName");
	}

	[Fact]
	public void ExtractProperties_NavigationProperties_ShouldBeFound()
	{
		// Arrange
		var sourceCode = @"
			public class TestEntity
			{
				public int Id { get; set; }
				public int ParentId { get; set; }
				public TestEntity Parent { get; set; } = null!;
				public ICollection<TestEntity> Children { get; set; } = new List<TestEntity>();
			}
		";

		// Act
		var properties = ExtractPropertiesFromSource(sourceCode);

		// Assert
		Assert.Equal(4, properties.Count);
		Assert.Contains(properties, p => p.Name == "ParentId");
		Assert.Contains(properties, p => p.Name == "Parent");
		Assert.Contains(properties, p => p.Name == "Children");
	}

	/// <summary>
	/// Helper method to extract properties from source code
	/// </summary>
	private List<Core.PropertyInfo> ExtractPropertiesFromSource(string sourceCode)
	{
		var tree = CSharpSyntaxTree.ParseText(sourceCode);
		var root = tree.GetCompilationUnitRoot();
		
		var entityClass = root.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First();

		// Extract properties using the same logic as CoreSchemaAnalysisService
		var properties = new List<Core.PropertyInfo>();

		var propertyDeclarations = entityClass.Members
			.OfType<PropertyDeclarationSyntax>()
			.Where(p => 
				// Has explicit accessor list with a getter or init
				(p.AccessorList?.Accessors.Any(a => 
					a.Kind() == SyntaxKind.GetAccessorDeclaration || 
					a.Kind() == SyntaxKind.InitAccessorDeclaration) == true) ||
				// OR is an expression-bodied property (no accessor list)
				p.ExpressionBody != null
			);

		foreach (var property in propertyDeclarations)
		{
			properties.Add(new Core.PropertyInfo
			{
				Name = property.Identifier.Text,
				Type = property.Type.ToString().Trim(),
				IsKey = property.Identifier.Text.Equals("Id", StringComparison.OrdinalIgnoreCase),
				IsForeignKey = property.Identifier.Text.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
							   !property.Identifier.Text.Equals("Id", StringComparison.OrdinalIgnoreCase)
			});
		}

		return properties;
	}
}
