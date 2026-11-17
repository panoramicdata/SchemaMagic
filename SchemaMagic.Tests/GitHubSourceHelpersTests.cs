using SchemaMagic.Core;
using Xunit;

namespace SchemaMagic.Tests;

public class GitHubSourceHelpersTests
{
	[Theory]
	[InlineData("https://github.com/panoramicdata/SchemaMagic", "panoramicdata", "SchemaMagic")]
	[InlineData("https://github.com/owner/repo/tree/main/src", "owner", "repo")]
	[InlineData("https://github.com/owner/repo/", "owner", "repo")]
	public void ParseRepositoryUrl_ReadsOwnerAndName(string url, string owner, string name) =>
		Assert.Equal((owner, name), GitHubSourceHelpers.ParseRepositoryUrl(url));

	[Theory]
	[InlineData("https://example.com/owner/repo")]
	[InlineData("https://github.com/owner")]
	[InlineData("not a url")]
	[InlineData("")]
	public void ParseRepositoryUrl_RejectsOtherInput(string url) =>
		Assert.Null(GitHubSourceHelpers.ParseRepositoryUrl(url));

	[Theory]
	[InlineData("public class AppDbContext : DbContext { public DbSet<A> As { get; set; } }", true)]
	[InlineData("public class AppDbContext : DbContext { }", false)]
	[InlineData("public class Plain { public DbSet<A> As { get; set; } }", false)]
	public void IsDbContextFile_NeedsContextAndDbSet(string content, bool expected) =>
		Assert.Equal(expected, GitHubSourceHelpers.IsDbContextFile(content));

	[Theory]
	[InlineData("src/Data/AppDbContext.cs", true)]
	[InlineData("src/Data/appcontext.CS", true)]
	[InlineData("src/Data/AppDbContext.txt", false)]
	[InlineData("src/Data/Order.cs", false)]
	public void IsDbContextCandidatePath_NeedsCSharpFileNamedContext(string path, bool expected) =>
		Assert.Equal(expected, GitHubSourceHelpers.IsDbContextCandidatePath(path));

	[Fact]
	public void ExtractEntityNames_ListsDbSetTypes()
	{
		const string source = """
			public class AppDbContext : DbContext
			{
				public DbSet<Order> Orders { get; set; }
				public DbSet<Customer> Customers { get; set; }
				public List<Other> NotADbSet { get; set; }
			}
			""";

		Assert.Equal(["Order", "Customer"], GitHubSourceHelpers.ExtractEntityNames(source));
	}

	[Fact]
	public void ExtractEntityNames_WithoutDbContextClass_IsEmpty() =>
		Assert.Empty(GitHubSourceHelpers.ExtractEntityNames("public class Plain { public DbSet<A> As { get; set; } }"));

	[Theory]
	[InlineData("public class Order { }", "Order", true)]
	[InlineData("namespace N { public sealed class Order : Base { } }", "Order", true)]
	[InlineData("public class OrderLine { }", "Order", false)]
	[InlineData("public record Order;", "Order", false)]
	public void ContainsClassDefinition_MatchesExactClassNames(string content, string className, bool expected) =>
		Assert.Equal(expected, GitHubSourceHelpers.ContainsClassDefinition(content, className));

	[Theory]
	[InlineData("src/Models/Order.cs", 0)]
	[InlineData("src/Entities/Order.cs", 1)]
	[InlineData("src/Domain/Order.cs", 2)]
	[InlineData("src/Data/Order.cs", 3)]
	[InlineData("src/Other/Order.cs", 4)]
	[InlineData("SRC/MODELS/Order.cs", 0)]
	public void EntityDirectoryPriority_RanksLikelyFoldersFirst(string path, int expected) =>
		Assert.Equal(expected, GitHubSourceHelpers.EntityDirectoryPriority(path));
}
