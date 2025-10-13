using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

public class SchemaAnalysisTests
{
	[Fact]
	public void Analyze_TestDbContext_ShouldFindAllEntities()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success, $"Analysis failed: {result.ErrorMessage}");
		Assert.Equal(12, result.EntitiesFound); // Company, Department, User, Project, Task, Document, Comment, Attachment, Tag, TaskTag, UserProject, AuditLog
	}

	[Fact]
	public void Analyze_TestDbContext_AllEntitiesShouldHaveProperties()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		Assert.NotNull(result.Entities);
		
		foreach (var entity in result.Entities)
		{
			Assert.True(entity.Value.Properties.Count > 2, 
				$"Entity {entity.Key} only has {entity.Value.Properties.Count} properties (expected more than 2)");
		}
	}

	[Fact]
	public void Analyze_UserEntity_ShouldHaveExpectedPropertyCount()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		var userEntity = result.Entities!["User"];
		
		// User has 40+ properties in TestDbContext.cs
		Assert.True(userEntity.Properties.Count >= 40, 
			$"User entity has {userEntity.Properties.Count} properties, expected at least 40");
	}

	[Fact]
	public void Analyze_CompanyEntity_ShouldHaveNavigationProperties()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		var companyEntity = result.Entities!["Company"];
		
		// Company should have Departments and Projects navigation properties
		var navProperties = companyEntity.Properties.Where(p => 
			p.Type.Contains("ICollection") || 
			p.Type.Contains("List")).ToList();
			
		Assert.True(navProperties.Count >= 2, 
			$"Company has {navProperties.Count} navigation properties, expected at least 2 (Departments, Projects)");
	}

	[Fact]
	public void Analyze_TaskEntity_ShouldHaveForeignKeys()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		var taskEntity = result.Entities!["Task"];
		
		// Task should have ProjectId, AssignedToId, CreatedById as foreign keys
		var foreignKeys = taskEntity.Properties.Where(p => p.IsForeignKey).ToList();
		
		Assert.True(foreignKeys.Count >= 3, 
			$"Task has {foreignKeys.Count} foreign keys, expected at least 3");
	}

	[Fact]
	public void Analyze_AllEntities_ShouldHavePrimaryKey()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		
		foreach (var entity in result.Entities!)
		{
			var hasKey = entity.Value.Properties.Any(p => p.IsKey);
			Assert.True(hasKey, $"Entity {entity.Key} has no primary key");
		}
	}

	[Fact]
	public void Analyze_PropertiesWithComments_ShouldExtractComments()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		var companyEntity = result.Entities!["Company"];
		
		// Company properties have comments
		var propertiesWithComments = companyEntity.Properties
			.Where(p => !string.IsNullOrEmpty(p.Comment))
			.ToList();
			
		Assert.True(propertiesWithComments.Count > 5, 
			$"Only {propertiesWithComments.Count} properties have comments, expected more than 5");
	}

	[Fact]
	public void Analyze_EntityWithComment_ShouldExtractEntityComment()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		var companyEntity = result.Entities!["Company"];
		
		// Company has entity-level comment
		Assert.False(string.IsNullOrEmpty(companyEntity.Comment), 
			"Company entity should have a comment");
	}

	[Fact]
	public void Analyze_ManyToManyEntity_ShouldHaveCompositeKey()
	{
		// Arrange
		var dbContextPath = GetTestDbContextPath();
		
		// Act
		var result = CoreSchemaAnalysisService.AnalyzeDbContextFile(dbContextPath);
		
		// Assert
		Assert.True(result.Success);
		
		// TaskTag should have TaskId and TagId as foreign keys
		if (result.Entities!.TryGetValue("TaskTag", out var taskTagEntity))
		{
			var foreignKeys = taskTagEntity.Properties.Where(p => p.IsForeignKey).ToList();
			Assert.True(foreignKeys.Count >= 2, 
				$"TaskTag has {foreignKeys.Count} foreign keys, expected at least 2");
		}
	}

	private static string GetTestDbContextPath()
	{
		// Start from test assembly location
		var testAssemblyPath = typeof(SchemaAnalysisTests).Assembly.Location;
		var directory = Path.GetDirectoryName(testAssemblyPath);
		
		// Navigate up to find solution root
		while (directory != null && !Directory.GetFiles(directory, "*.sln").Any())
		{
			directory = Directory.GetParent(directory)?.FullName;
		}
		
		if (directory == null)
		{
			throw new InvalidOperationException("Could not find solution root");
		}
		
		var testDbContextPath = Path.Combine(directory, "TestDbContext.cs");
		
		if (!File.Exists(testDbContextPath))
		{
			throw new FileNotFoundException($"TestDbContext.cs not found at: {testDbContextPath}");
		}
		
		return testDbContextPath;
	}
}
