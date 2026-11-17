using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

/// <summary>
/// Verifies that all modern C# property patterns are extracted by the real analysis service.
/// </summary>
public class PropertyExtractionTests
{
	private const string DbContextSource = """
		public class TestDbContext : DbContext
		{
			public DbSet<TestEntity> Entities { get; set; }
		}
		""";

	public static TheoryData<string, string[]> PropertyPatterns => new()
	{
		{ "public int Id { get; set; } public string Name { get; set; }", ["Id", "Name"] },
		{ "public required string Name { get; set; } public required int Age { get; init; }", ["Name", "Age"] },
		{ "public int Id { get; init; } public string Name { get; init; }", ["Id", "Name"] },
		{
			"public string FirstName { get; set; } public string LastName { get; set; } public string FullName => $\"{FirstName} {LastName}\";",
			["FirstName", "LastName", "FullName"]
		},
		{
			"public int Id { get; set; } public required string Name { get; set; } public string? Description { get; init; } public DateTime CreatedAt { get; } = DateTime.Now; public string DisplayName => Name.ToUpper();",
			["Id", "Name", "Description", "CreatedAt", "DisplayName"]
		},
		{
			"public int Id { get; set; } public int ParentId { get; set; } public TestEntity Parent { get; set; } = null!; public ICollection<TestEntity> Children { get; set; } = new List<TestEntity>();",
			["Id", "ParentId", "Parent", "Children"]
		},
		{ "public int WriteOnly { set { } } public int Readable { get; set; }", ["Readable"] },
		{ "private int _field; public int Value => _field;", ["Value"] }
	};

	private static List<PropertyInfo> Extract(string members)
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			DbContextSource,
			"TestDbContext.cs",
			new Dictionary<string, string> { ["TestEntity.cs"] = $"public class TestEntity {{ {members} }}" });

		Assert.True(result.Success, result.ErrorMessage);
		return result.Entities["TestEntity"].Properties;
	}

	[Theory]
	[MemberData(nameof(PropertyPatterns))]
	public void ExtractProperties_FindsEveryReadableProperty(string members, string[] expectedNames) =>
		Assert.Equal(expectedNames, Extract(members).Select(p => p.Name));

	[Fact]
	public void ExtractProperties_PreservesNullability()
	{
		var properties = Extract("public string Name { get; set; } public string? OptionalName { get; set; } public int? OptionalAge { get; set; }");

		Assert.Equal(["string", "string?", "int?"], properties.Select(p => p.Type));
	}

	[Fact]
	public void ExtractProperties_DetectsKeysAndHeuristicForeignKeys()
	{
		var properties = Extract("public int Id { get; set; } public int ParentId { get; set; } public string OwnerId { get; set; } public Guid TenantId { get; set; }");

		Assert.Equal([true, false, false, false], properties.Select(p => p.IsKey));
		Assert.Equal([false, true, false, true], properties.Select(p => p.IsForeignKey));
	}
}
