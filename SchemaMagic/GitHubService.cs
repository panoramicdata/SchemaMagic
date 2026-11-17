using Octokit;
using SchemaMagic.Core;
using System.Text;

namespace SchemaMagic;

public class GitHubService
{
	private const int MaxFilesToScan = 500;

	private readonly GitHubClient _gitHubClient;

	public GitHubService() : this(null)
	{
	}

	public GitHubService(string? accessToken)
	{
		_gitHubClient = new GitHubClient(new ProductHeaderValue("SchemaMagic-CLI"));

		if (!string.IsNullOrEmpty(accessToken))
		{
			_gitHubClient.Credentials = new Credentials(accessToken);
		}
	}

	public async Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repoUrl)
	{
		var (owner, name) = GitHubSourceHelpers.ParseRepositoryUrl(repoUrl)
			?? throw new ArgumentException("Invalid GitHub repository URL");

		try
		{
			return await AnalyzeAsync(owner, name);
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

	// Kept for backward compatibility
	public async Task<List<DbContextFileInfo>> FindDbContextFilesAsync(string repoUrl)
	{
		var result = await AnalyzeRepositoryAsync(repoUrl);
		return result.DbContextFiles;
	}

	private async Task<RepositoryAnalysisResult> AnalyzeAsync(string owner, string name)
	{
		Console.WriteLine($"🔍 Accessing repository: {owner}/{name}");
		var repository = await _gitHubClient.Repository.Get(owner, name);
		var tree = await _gitHubClient.Git.Tree.GetRecursive(owner, name, repository.DefaultBranch);
		Console.WriteLine($"📁 Default branch {repository.DefaultBranch} has {tree.Tree.Count} files");

		var result = new RepositoryAnalysisResult
		{
			Repository = $"{owner}/{name}",
			DbContextFiles = await FindDbContextFilesAsync(owner, name, tree)
		};

		foreach (var dbContextFile in result.DbContextFiles)
		{
			var entityNames = GitHubSourceHelpers.ExtractEntityNames(dbContextFile.Content);
			Console.WriteLine($"🔍 {dbContextFile.FileName}: {entityNames.Count} entity types");

			var entityFiles = await FindEntityFilesAsync(owner, name, tree, entityNames);
			foreach (var (entityName, content) in entityFiles)
			{
				result.EntityFiles.TryAdd(entityName, content);
			}
		}

		return result;
	}

	private async Task<List<DbContextFileInfo>> FindDbContextFilesAsync(string owner, string repo, TreeResponse tree)
	{
		var dbContextFiles = new List<DbContextFileInfo>();
		var candidates = tree.Tree
			.Where(item => item.Type == TreeType.Blob && GitHubSourceHelpers.IsDbContextCandidatePath(item.Path))
			.ToList();

		Console.WriteLine($"📁 Found {candidates.Count} potential DbContext files");

		foreach (var file in candidates)
		{
			var content = await TryGetFileContentAsync(owner, repo, file.Path);
			if (content is not null && GitHubSourceHelpers.IsDbContextFile(content))
			{
				dbContextFiles.Add(new DbContextFileInfo
				{
					FileName = Path.GetFileName(file.Path),
					FilePath = file.Path,
					Content = content,
					Repository = $"{owner}/{repo}"
				});
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
		var sourceFiles = tree.Tree
			.Where(item => item.Type == TreeType.Blob && item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			.ToList();

		var entityFiles = await FindEntityFilesByNameAsync(owner, repo, sourceFiles, entityNames);

		if (entityFiles.Count < entityNames.Count)
		{
			var remaining = entityNames.Where(e => !entityFiles.ContainsKey(e)).ToHashSet();
			await SearchSourceFilesForEntitiesAsync(owner, repo, sourceFiles, remaining, entityFiles);
		}

		Console.WriteLine($"   📊 Found {entityFiles.Count}/{entityNames.Count} entity files");
		return entityFiles;
	}

	// Strategy 1: files named exactly like the entity
	private async Task<Dictionary<string, string>> FindEntityFilesByNameAsync(
		string owner,
		string repo,
		List<TreeItem> sourceFiles,
		List<string> entityNames)
	{
		var entityFiles = new Dictionary<string, string>();
		var entitySet = entityNames.ToHashSet();

		foreach (var file in sourceFiles.Where(f => entitySet.Contains(Path.GetFileNameWithoutExtension(f.Path))))
		{
			var className = Path.GetFileNameWithoutExtension(file.Path);
			var content = await TryGetFileContentAsync(owner, repo, file.Path);
			if (content is not null && GitHubSourceHelpers.ContainsClassDefinition(content, className))
			{
				entityFiles[className] = content;
			}
		}

		return entityFiles;
	}

	// Strategy 2: scan source files for class definitions, likeliest folders first
	private async Task SearchSourceFilesForEntitiesAsync(
		string owner,
		string repo,
		List<TreeItem> sourceFiles,
		HashSet<string> remaining,
		Dictionary<string, string> entityFiles)
	{
		var filesToScan = sourceFiles
			.Where(item => !item.Path.Contains("/bin/") && !item.Path.Contains("/obj/"))
			.OrderBy(item => GitHubSourceHelpers.EntityDirectoryPriority(item.Path))
			.ThenBy(item => item.Path)
			.Take(MaxFilesToScan);

		Console.WriteLine($"   🔍 {remaining.Count} entities still missing, scanning source files");

		try
		{
			foreach (var file in filesToScan.TakeWhile(_ => remaining.Count > 0))
			{
				var content = await TryGetFileContentAsync(owner, repo, file.Path);
				if (content is not null)
				{
					MoveFoundEntities(content, remaining, entityFiles);
				}
			}
		}
		catch (RateLimitExceededException)
		{
			Console.WriteLine("   ⚠️ GitHub API rate limit reached. Consider providing --github-token");
		}
	}

	private static void MoveFoundEntities(string content, HashSet<string> remaining, Dictionary<string, string> entityFiles)
	{
		foreach (var entityName in remaining.Where(e => GitHubSourceHelpers.ContainsClassDefinition(content, e)).ToList())
		{
			entityFiles[entityName] = content;
			remaining.Remove(entityName);
		}
	}

	private async Task<string?> TryGetFileContentAsync(string owner, string repo, string path)
	{
		try
		{
			return await GetFileContentAsync(owner, repo, path);
		}
		catch (Exception ex) when (ex is not RateLimitExceededException)
		{
			Console.WriteLine($"   ⚠️ Error reading {path}: {ex.Message}");
			return null;
		}
	}

	private async Task<string> GetFileContentAsync(string owner, string repo, string path)
	{
		var fileContent = await _gitHubClient.Repository.Content.GetAllContents(owner, repo, path);
		var content = fileContent.FirstOrDefault();

		if (content?.Type == ContentType.File)
		{
			return content.EncodedContent is not null
				? Encoding.UTF8.GetString(Convert.FromBase64String(content.EncodedContent))
				: content.Content ?? string.Empty;
		}

		return string.Empty;
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
	public Dictionary<string, string> EntityFiles { get; set; } = [];
}
