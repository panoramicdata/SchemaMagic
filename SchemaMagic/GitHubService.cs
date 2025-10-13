using Octokit;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace SchemaMagic;

public class GitHubService
{
	private readonly GitHubClient _gitHubClient;

	public GitHubService(string? accessToken = null)
	{
		_gitHubClient = new GitHubClient(new ProductHeaderValue("SchemaMagic-CLI"));
		
		if (!string.IsNullOrEmpty(accessToken))
		{
			_gitHubClient.Credentials = new Credentials(accessToken);
		}
	}

	public async Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repoUrl)
	{
		var (owner, name) = ParseGitHubUrl(repoUrl) ?? throw new ArgumentException("Invalid GitHub repository URL");

		try
		{
			Console.WriteLine($"?? Accessing repository: {owner}/{name}");
			
			var repository = await _gitHubClient.Repository.Get(owner, name);
			Console.WriteLine($"? Repository accessed (Private: {repository.Private})");
			
			var defaultBranch = repository.DefaultBranch;
			Console.WriteLine($"?? Default branch: {defaultBranch}");
			
			// Get the tree recursively
			var tree = await _gitHubClient.Git.Tree.GetRecursive(owner, name, defaultBranch);
			Console.WriteLine($"?? Repository has {tree.Tree.Count} files");
			
			// Find DbContext files
			var dbContextFiles = await FindDbContextFilesAsync(owner, name, tree);
			
			if (dbContextFiles.Count == 0)
			{
				return new RepositoryAnalysisResult
				{
					Repository = $"{owner}/{name}",
					DbContextFiles = [],
					EntityFiles = new Dictionary<string, string>()
				};
			}

			Console.WriteLine($"? Found {dbContextFiles.Count} DbContext file(s)");

			// For each DbContext, find related entity files
			var allEntityFiles = new Dictionary<string, string>();
			
			foreach (var dbContextFile in dbContextFiles)
			{
				Console.WriteLine($"\n?? Analyzing DbContext: {dbContextFile.FileName}");
				
				// Parse DbContext to extract entity names
				var entityNames = ExtractEntityNamesFromDbContext(dbContextFile.Content);
				Console.WriteLine($"   ?? Found {entityNames.Count} entity types in DbContext");

				// Find entity model files
				var entityFiles = await FindEntityFilesAsync(owner, name, tree, entityNames);
				Console.WriteLine($"   ? Found {entityFiles.Count} entity class files");

				// Merge into overall collection
				foreach (var kvp in entityFiles)
				{
					if (!allEntityFiles.ContainsKey(kvp.Key))
					{
						allEntityFiles[kvp.Key] = kvp.Value;
					}
				}
			}

			return new RepositoryAnalysisResult
			{
				Repository = $"{owner}/{name}",
				DbContextFiles = dbContextFiles,
				EntityFiles = allEntityFiles
			};
		}
		catch (NotFoundException)
		{
			throw new InvalidOperationException($"Repository '{owner}/{name}' not found or you don't have access.");
		}
		catch (RateLimitExceededException)
		{
			throw new InvalidOperationException("GitHub API rate limit exceeded. Please provide a Personal Access Token with --github-token.");
		}
		catch (AuthorizationException)
		{
			throw new InvalidOperationException("Authorization failed. If this is a private repository, provide a Personal Access Token with --github-token.");
		}
	}

	private async Task<List<DbContextFileInfo>> FindDbContextFilesAsync(string owner, string repo, TreeResponse tree)
	{
		var dbContextFiles = new List<DbContextFileInfo>();

		// Filter for .cs files that might contain DbContext
		var csFiles = tree.Tree
			.Where(item => item.Type == TreeType.Blob)
			.Where(item => item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			.Where(item => item.Path.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
						   item.Path.Contains("Context", StringComparison.OrdinalIgnoreCase))
			.ToList();
		
		Console.WriteLine($"?? Found {csFiles.Count} potential DbContext files");
		
		foreach (var file in csFiles)
		{
			try
			{
				var content = await GetFileContentAsync(owner, repo, file.Path);
				
				if (IsDbContextFile(content))
				{
					Console.WriteLine($"   ? Valid DbContext: {file.Path}");
					dbContextFiles.Add(new DbContextFileInfo
					{
						FileName = Path.GetFileName(file.Path),
						FilePath = file.Path,
						Content = content,
						Repository = $"{owner}/{repo}"
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"   ?? Error reading {file.Path}: {ex.Message}");
			}
		}

		return dbContextFiles;
	}

	private async Task<Dictionary<string, string>> FindEntityFilesAsync(
		string owner, 
		string repo, 
		TreeResponse tree,
		List<string> entityNames)
	{
		var entityFiles = new Dictionary<string, string>();
		var entitySet = new HashSet<string>(entityNames);

		// Strategy 1: Look for files with matching names
		var potentialEntityFiles = tree.Tree
			.Where(item => item.Type == TreeType.Blob)
			.Where(item => item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			.Where(item =>
			{
				var fileName = Path.GetFileNameWithoutExtension(item.Path);
				return entitySet.Contains(fileName);
			})
			.ToList();

		Console.WriteLine($"   ?? Found {potentialEntityFiles.Count} files matching entity names");

		foreach (var file in potentialEntityFiles)
		{
			try
			{
				var content = await GetFileContentAsync(owner, repo, file.Path);
				var fileName = Path.GetFileNameWithoutExtension(file.Path);
				
				// Verify it's actually a class definition for our entity
				if (ContainsClassDefinition(content, fileName))
				{
					entityFiles[fileName] = content;
					Console.WriteLine($"      ? {fileName}: {file.Path}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"      ?? Error reading {file.Path}: {ex.Message}");
			}
		}

		// Strategy 2: Look in common model directories
		var modelDirectories = new[] { "Models", "Entities", "Domain", "Data" };
		var modelFiles = tree.Tree
			.Where(item => item.Type == TreeType.Blob)
			.Where(item => item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			.Where(item => modelDirectories.Any(dir => 
				item.Path.Contains($"/{dir}/", StringComparison.OrdinalIgnoreCase) ||
				item.Path.Contains($"\\{dir}\\", StringComparison.OrdinalIgnoreCase)))
			.ToList();

		Console.WriteLine($"   ?? Checking {modelFiles.Count} files in model directories");

		foreach (var file in modelFiles)
		{
			try
			{
				var fileName = Path.GetFileNameWithoutExtension(file.Path);
				
				// Skip if we already have this entity
				if (entityFiles.ContainsKey(fileName))
					continue;

				// Only fetch if it might be one of our entities
				if (!entitySet.Contains(fileName))
					continue;

				var content = await GetFileContentAsync(owner, repo, file.Path);
				
				if (ContainsClassDefinition(content, fileName))
				{
					entityFiles[fileName] = content;
					Console.WriteLine($"      ? {fileName}: {file.Path}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"      ?? Error reading {file.Path}: {ex.Message}");
			}
		}

		return entityFiles;
	}

	private List<string> ExtractEntityNamesFromDbContext(string dbContextContent)
	{
		var entityNames = new List<string>();
		
		try
		{
			var tree = CSharpSyntaxTree.ParseText(dbContextContent);
			var root = tree.GetCompilationUnitRoot();

			// Find DbContext class
			var dbContextClass = root.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.FirstOrDefault(c => c.BaseList?.Types.Any(t => t.ToString().Contains("DbContext")) == true);

			if (dbContextClass != null)
			{
				// Extract DbSet properties
				var dbSetProperties = dbContextClass.Members
					.OfType<PropertyDeclarationSyntax>()
					.Where(p => p.Type.ToString().StartsWith("DbSet<", StringComparison.OrdinalIgnoreCase))
					.ToList();

				foreach (var dbSetProperty in dbSetProperties)
				{
					var match = Regex.Match(dbSetProperty.Type.ToString(), @"DbSet<(.+?)>");
					if (match.Success)
					{
						var entityName = match.Groups[1].Value.Trim();
						entityNames.Add(entityName);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"   ?? Error parsing DbContext: {ex.Message}");
		}

		return entityNames;
	}

	private bool ContainsClassDefinition(string content, string className)
	{
		try
		{
			var tree = CSharpSyntaxTree.ParseText(content);
			var root = tree.GetCompilationUnitRoot();

			var classDecl = root.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.FirstOrDefault(c => c.Identifier.Text == className);

			return classDecl != null;
		}
		catch
		{
			return false;
		}
	}

	// Keep backward compatibility
	public async Task<List<DbContextFileInfo>> FindDbContextFilesAsync(string repoUrl)
	{
		var result = await AnalyzeRepositoryAsync(repoUrl);
		return result.DbContextFiles;
	}

	private async Task<string> GetFileContentAsync(string owner, string repo, string path)
	{
		var fileContent = await _gitHubClient.Repository.Content.GetAllContents(owner, repo, path);
		var content = fileContent.FirstOrDefault();

		if (content?.Type == ContentType.File)
		{
			return content.EncodedContent != null
				? Encoding.UTF8.GetString(Convert.FromBase64String(content.EncodedContent))
				: content.Content ?? string.Empty;
		}

		return string.Empty;
	}

	private static (string Owner, string Name)? ParseGitHubUrl(string url)
	{
		try
		{
			var uri = new Uri(url);
			if (uri.Host != "github.com")
				return null;

			var segments = uri.AbsolutePath.Trim('/').Split('/');
			if (segments.Length >= 2)
			{
				return (segments[0], segments[1]);
			}
		}
		catch
		{
			return null;
		}

		return null;
	}

	private static bool IsDbContextFile(string content)
	{
		return content.Contains("DbContext", StringComparison.OrdinalIgnoreCase) &&
			   (content.Contains("class", StringComparison.OrdinalIgnoreCase) || 
			    content.Contains("public", StringComparison.OrdinalIgnoreCase)) &&
			   content.Contains("DbSet", StringComparison.OrdinalIgnoreCase);
	}
}

public class DbContextFileInfo
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
}

public class RepositoryAnalysisResult
{
	public string Repository { get; set; } = string.Empty;
	public List<DbContextFileInfo> DbContextFiles { get; set; } = [];
	public Dictionary<string, string> EntityFiles { get; set; } = new();
}
