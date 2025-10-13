using Octokit;
using System.Text;

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

	public async Task<List<DbContextFileInfo>> FindDbContextFilesAsync(string repoUrl)
	{
		var (owner, name) = ParseGitHubUrl(repoUrl) ?? throw new ArgumentException("Invalid GitHub repository URL");
		var dbContextFiles = new List<DbContextFileInfo>();

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
					var content = await GetFileContentAsync(owner, name, file.Path);
					
					if (IsDbContextFile(content))
					{
						Console.WriteLine($"   ? Valid DbContext: {file.Path}");
						dbContextFiles.Add(new DbContextFileInfo
						{
							FileName = Path.GetFileName(file.Path),
							FilePath = file.Path,
							Content = content,
							Repository = $"{owner}/{name}"
						});
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"   ?? Error reading {file.Path}: {ex.Message}");
				}
			}
			
			Console.WriteLine($"? Found {dbContextFiles.Count} DbContext files");
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

		return dbContextFiles;
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
