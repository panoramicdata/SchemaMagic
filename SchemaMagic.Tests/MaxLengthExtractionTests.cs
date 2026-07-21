using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

/// <summary>
/// Verifies that column max lengths (from [MaxLength] / [StringLength] attributes) are extracted
/// into PropertyInfo.MaxLength so the schema documentation can show "type(n)" (MS-21560).
/// </summary>
public class MaxLengthExtractionTests
{
	private const string DbContext = @"
		using Microsoft.EntityFrameworkCore;
		public class SampleDbContext : DbContext
		{
			public DbSet<Widget> Widgets { get; set; }
		}
	";

	private const string WidgetEntity = @"
		using System.ComponentModel.DataAnnotations;
		public class Widget
		{
			public int Id { get; set; }

			[MaxLength(255)]
			public string Name { get; set; } = string.Empty;

			[StringLength(64)]
			public string? Code { get; set; }

			public string Description { get; set; } = string.Empty;

			[MaxLength(1024)]
			public string? Notes { get; set; }
		}
	";

	private static PropertyInfo GetProperty(string name)
	{
		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			DbContext,
			"SampleDbContext.cs",
			new Dictionary<string, string> { ["Widget.cs"] = WidgetEntity });

		Assert.True(result.Success, $"Analysis failed: {result.ErrorMessage}");
		var widget = result.Entities!["Widget"];
		return widget.Properties.First(p => p.Name == name);
	}

	[Fact]
	public void MaxLengthAttribute_SetsMaxLength()
		=> Assert.Equal(255, GetProperty("Name").MaxLength);

	[Fact]
	public void StringLengthAttribute_SetsMaxLength()
		=> Assert.Equal(64, GetProperty("Code").MaxLength);

	[Fact]
	public void UnboundedString_HasNoMaxLength()
		=> Assert.Null(GetProperty("Description").MaxLength);

	[Fact]
	public void MaxLengthAttribute_OnNullableString_SetsMaxLength()
		=> Assert.Equal(1024, GetProperty("Notes").MaxLength);
}
