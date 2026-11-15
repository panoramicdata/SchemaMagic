using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

public class DiscoveryTests
{
	private const string ShopContext = """
		using Microsoft.EntityFrameworkCore;
		public class ShopDbContext : DbContext
		{
			public DbSet<Order> Orders { get; set; }
		}
		""";

	private const string OrderClass = """
		public class Order
		{
			public int Id { get; set; }
			public int CustomerRef { get; set; }
			public int CategoryId { get; set; }
		}
		""";

	private static string Snapshot(string entityName, string foreignKey) => $$"""
		using Microsoft.EntityFrameworkCore;
		partial class ShopDbContextModelSnapshot : ModelSnapshot
		{
			protected override void BuildModel(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity("Shop.Models.{{entityName}}", b =>
				{
					b.HasOne("Shop.Models.Other", null).WithMany().HasForeignKey("{{foreignKey}}")
				});
			}
		}
		""";

	private static SchemaAnalysisResult AnalyzeSucceeding(string contextPath)
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(contextPath);
		Assert.True(result.Success, result.ErrorMessage);
		return result;
	}

	private static bool IsForeignKey(SchemaAnalysisResult result, string entity, string property) =>
		result.Entities[entity].Properties.Single(p => p.Name == property).IsForeignKey;

	[Fact]
	public void Snapshot_ForeignKeys_AreAppliedToProperties()
	{
		using var directory = new TempDirectory();
		var contextPath = directory.Write("ShopDbContext.cs", ShopContext + OrderClass);
		directory.Write("Migrations/ShopDbContextModelSnapshot.cs", Snapshot("Order", "CustomerRef"));

		var result = AnalyzeSucceeding(contextPath);

		Assert.True(IsForeignKey(result, "Order", "CustomerRef"));
		Assert.True(IsForeignKey(result, "Order", "CategoryId"));
	}

	[Fact]
	public void WithoutSnapshot_OnlyHeuristicForeignKeysAreDetected()
	{
		using var directory = new TempDirectory();
		var contextPath = directory.Write("ShopDbContext.cs", ShopContext + OrderClass);

		var result = AnalyzeSucceeding(contextPath);

		Assert.False(IsForeignKey(result, "Order", "CustomerRef"));
		Assert.True(IsForeignKey(result, "Order", "CategoryId"));
	}

	[Fact]
	public void Snapshot_InProviderSubdirectory_IsPreferredOverRoot()
	{
		using var directory = new TempDirectory();
		var contextPath = directory.Write("ShopDbContext.cs", ShopContext + OrderClass);
		directory.Write("Migrations/ShopDbContextModelSnapshot.cs", Snapshot("Order", "CustomerRef"));
		directory.Write("Migrations/Sqlite/SqliteModelSnapshot.cs", Snapshot("Order", "CategoryId"));

		var result = AnalyzeSucceeding(contextPath);

		Assert.False(IsForeignKey(result, "Order", "CustomerRef"));
	}

	[Fact]
	public void MissingEntityClass_GetsFallbackWithSnapshotForeignKeys()
	{
		using var directory = new TempDirectory();
		var context = ShopContext.Replace("DbSet<Order> Orders", "DbSet<Ghost> Ghosts");
		var contextPath = directory.Write("ShopDbContext.cs", context);
		directory.Write("Migrations/ShopDbContextModelSnapshot.cs", Snapshot("Ghost", "OwnerRef"));

		var result = AnalyzeSucceeding(contextPath);

		var names = result.Entities["Ghost"].Properties.Select(p => p.Name).ToList();
		Assert.Equal(["Id", "OwnerRef", "Name"], names);
		Assert.True(IsForeignKey(result, "Ghost", "OwnerRef"));
	}

	[Fact]
	public void MissingEntityClass_WithoutSnapshot_GetsIdAndName()
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			ShopContext,
			"ShopDbContext.cs",
			[]);

		Assert.True(result.Success, result.ErrorMessage);
		Assert.Equal(["Id", "Name"], result.Entities["Order"].Properties.Select(p => p.Name));
	}

	[Fact]
	public void ModelsDirectory_ProvidesEntityClasses()
	{
		using var directory = new TempDirectory();
		var contextPath = directory.Write("ShopDbContext.cs", ShopContext);
		directory.Write("Models/Order.cs", OrderClass);

		var result = AnalyzeSucceeding(contextPath);

		Assert.Contains(result.Entities["Order"].Properties, p => p.Name == "CustomerRef");
	}

	[Fact]
	public void SolutionDataProject_ProvidesEntityClasses_ExcludingBuildOutput()
	{
		using var directory = new TempDirectory();
		directory.Write("Shop.sln", "");
		directory.Write("MagicSuite.Data/Models/Order.cs", OrderClass);
		directory.Write("MagicSuite.Data/obj/Order.cs", OrderClass.Replace("CustomerRef", "FromObj"));
		var contextPath = directory.Write("src/Context/ShopDbContext.cs", ShopContext);

		var result = AnalyzeSucceeding(contextPath);

		var names = result.Entities["Order"].Properties.Select(p => p.Name).ToList();
		Assert.Contains("CustomerRef", names);
		Assert.DoesNotContain("FromObj", names);
	}

	[Fact]
	public void Inheritance_BaseClassPropertiesAreInherited()
	{
		var entities = $$"""
			public class BaseEntity { public int Id { get; set; } public string Audit { get; set; } }
			public class Order : BaseEntity { public int Total { get; set; } }
			""";

		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			ShopContext,
			"ShopDbContext.cs",
			new Dictionary<string, string> { ["Order.cs"] = entities });

		var order = result.Entities["Order"];
		Assert.Equal("BaseEntity", order.BaseType);
		Assert.Contains(order.InheritedProperties, p => p.Name == "Audit");
		Assert.DoesNotContain(order.Properties, p => p.Name == "Audit");
	}

	[Theory]
	[InlineData("object")]
	[InlineData("System.ValueType")]
	[InlineData("MissingBase")]
	public void Inheritance_UnusableBaseTypes_LeaveNoInheritedProperties(string baseType)
	{
		var entity = $"public class Order : {baseType} {{ public int Id {{ get; set; }} }}";

		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			ShopContext,
			"ShopDbContext.cs",
			new Dictionary<string, string> { ["Order.cs"] = entity });

		var order = result.Entities["Order"];
		Assert.Empty(order.InheritedProperties);
		Assert.Equal(baseType == "MissingBase" ? baseType : string.Empty, order.BaseType);
	}

	[Fact]
	public void Comments_PreferAttributesOverDocumentation()
	{
		var entity = """
			/// <summary>
			/// A customer order.
			/// </summary>
			public class Order
			{
				/// <summary>The   key
				/// value.</summary>
				public int Id { get; set; }

				/// <summary>Ignored summary</summary>
				[Comment("Attribute wins")]
				public int Total { get; set; }
			}
			""";

		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			ShopContext,
			"ShopDbContext.cs",
			new Dictionary<string, string> { ["Order.cs"] = entity });

		var order = result.Entities["Order"];
		Assert.Equal("A customer order.", order.Comment);
		Assert.Equal("The key value.", order.Properties.Single(p => p.Name == "Id").Comment);
		Assert.Equal("Attribute wins", order.Properties.Single(p => p.Name == "Total").Comment);
	}

	[Fact]
	public void AnalyzeDbContextFile_MissingFile_ReportsFailure()
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".cs"));

		Assert.False(result.Success);
		Assert.Contains("not found", result.ErrorMessage);
		Assert.Equal(0, result.EntitiesFound);
	}

	[Fact]
	public void AnalyzeDbContextWithEntityFiles_WithoutDbContextClass_ReportsFailure()
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles("public class NotAContext { }", "x.cs", []);

		Assert.False(result.Success);
		Assert.Contains("No DbContext class", result.ErrorMessage);
	}

	[Fact]
	public void AnalyzeDbContextWithEntityFiles_KeepsTheSuppliedGuid()
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(ShopContext, "ShopDbContext.cs", [], "fixed-guid");

		Assert.Equal("fixed-guid", result.DocumentGuid);
		Assert.Contains("\"fixed-guid\"", result.HtmlContent);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AnalyzeDbContextContent_AcceptsRelativeAndRootedFileNames(bool rooted)
	{
		using var directory = new TempDirectory();
		var fileName = rooted ? Path.Combine(directory.Path, "ShopDbContext.cs") : "ShopDbContext.cs";

		var result = CoreSchemaAnalysisService.AnalyzeDbContextContent(ShopContext + OrderClass, fileName);

		Assert.True(result.Success, result.ErrorMessage);
		Assert.Equal(1, result.EntitiesFound);
		Assert.False(string.IsNullOrEmpty(result.DocumentGuid));
	}}
