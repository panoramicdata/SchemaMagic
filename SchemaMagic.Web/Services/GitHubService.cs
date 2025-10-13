using Octokit;
using System.Text;

namespace SchemaMagic.Web.Services;

public class GitHubService(HttpClient httpClient)
{
	private readonly GitHubClient _gitHubClient = new GitHubClient(new ProductHeaderValue("SchemaMagic-Web"));
	private readonly HttpClient _httpClient = httpClient;

	// GitHub Personal Access Token for private repositories
	public void SetAccessToken(string accessToken)
	{
		if (!string.IsNullOrEmpty(accessToken))
		{
			_gitHubClient.Credentials = new Credentials(accessToken);
		}
	}

	public async Task<List<DbContextInfo>> FindDbContextFilesAsync(string repoUrl, string? accessToken = null)
	{
		var (Owner, Name) = ParseGitHubUrl(repoUrl) ?? throw new ArgumentException("Invalid GitHub repository URL");
		var dbContextFiles = new List<DbContextInfo>();

		// Set access token if provided
		if (!string.IsNullOrEmpty(accessToken))
		{
			SetAccessToken(accessToken);
		}

		try
		{
			// First, check if we can access the repository
			Repository repository;
			try
			{
				repository = await _gitHubClient.Repository.Get(Owner, Name);
				
				// Check if repository is accessible
				if (repository.Private && string.IsNullOrEmpty(accessToken))
				{
					throw new InvalidOperationException($"Repository '{Owner}/{Name}' is private. Please provide a Personal Access Token with repository read permissions.");
				}
				
				Console.WriteLine($"? Repository accessed: {Owner}/{Name} (Private: {repository.Private})");
			}
			catch (NotFoundException)
			{
				// Repository not found - give specific error
				throw new InvalidOperationException($"Repository '{Owner}/{Name}' does not exist or you don't have permission to access it. Please check the URL and try again.");
			}
			catch (RateLimitExceededException)
			{
				throw new InvalidOperationException("GitHub API rate limit exceeded. Please provide a Personal Access Token for higher limits, or try again later.");
			}
			catch (AuthorizationException)
			{
				throw new InvalidOperationException("Authorization failed. If this is a private repository, please provide a valid Personal Access Token with repository read permissions.");
			}

			// Try Code Search API first (faster but requires auth for many operations)
			// If it fails, fall back to tree search
			bool useCodeSearch = !string.IsNullOrEmpty(accessToken);
			
			if (useCodeSearch)
			{
				try
				{
					var searchRequest = new SearchCodeRequest("DbContext")
					{
						Repos = new RepositoryCollection { $"{Owner}/{Name}" },
						Extensions = new[] { "cs" }
					};

					var searchResult = await _gitHubClient.Search.SearchCode(searchRequest);
					
					Console.WriteLine($"?? Code search found {searchResult.TotalCount} files");

					foreach (var file in searchResult.Items)
					{
						var fileContent = await GetFileContentAsync(Owner, Name, file.Path);

						if (IsDbContextFile(fileContent))
						{
							Console.WriteLine($"? Found DbContext: {file.Path}");
							dbContextFiles.Add(new DbContextInfo
							{
								FileName = Path.GetFileName(file.Path),
								FilePath = file.Path,
								Content = fileContent,
								Repository = $"{Owner}/{Name}"
							});
						}
					}
					
					// If code search worked, return results
					if (dbContextFiles.Any() || searchResult.TotalCount == 0)
					{
						return dbContextFiles;
					}
				}
				catch (ApiValidationException)
				{
					Console.WriteLine("?? Code search requires auth - falling back to tree search");
					// Fall through to tree search
				}
			}
			
			// Fall back to tree search (works without auth for public repos)
			Console.WriteLine($"?? Using tree search for {Owner}/{Name}");
			dbContextFiles = await FallbackTreeSearch(Owner, Name);
		}
		catch (RateLimitExceededException)
		{
			throw new InvalidOperationException("GitHub API rate limit exceeded. Please provide a Personal Access Token for higher limits, or try again later.");
		}
		catch (AuthorizationException)
		{
			throw new InvalidOperationException("Access denied. This appears to be a private repository. Please provide a valid Personal Access Token with repository read permissions.");
		}
		catch (Exception ex) when (ex.Message.Contains("API rate limit exceeded"))
		{
			throw new InvalidOperationException("GitHub API rate limit exceeded. Please provide a Personal Access Token for higher limits, or try again later.");
		}
		catch (InvalidOperationException)
		{
			// Re-throw our own exceptions
			throw;
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Error accessing GitHub repository: {ex.Message}");
		}

		return dbContextFiles;
	}

	private async Task<List<DbContextInfo>> FallbackTreeSearch(string owner, string repo)
	{
		var dbContextFiles = new List<DbContextInfo>();
		
		try
		{
			Console.WriteLine($"?? Starting tree search for {owner}/{repo}");
			
			// Get the default branch
			var repository = await _gitHubClient.Repository.Get(owner, repo);
			var defaultBranch = repository.DefaultBranch;
			
			Console.WriteLine($"   Default branch: {defaultBranch}");
			
			// Get the tree recursively
			var tree = await _gitHubClient.Git.Tree.GetRecursive(owner, repo, defaultBranch);
			
			Console.WriteLine($"   Repository has {tree.Tree.Count} files");
			
			// Filter for .cs files that might contain DbContext
			var csFiles = tree.Tree
				.Where(item => item.Type == TreeType.Blob)
				.Where(item => item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
				.Where(item => item.Path.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
							   item.Path.Contains("Context", StringComparison.OrdinalIgnoreCase))
				.ToList();
			
			Console.WriteLine($"   Found {csFiles.Count} potential DbContext files");
			
			foreach (var file in csFiles)
			{
				Console.WriteLine($"   Checking: {file.Path}");
				
				try
				{
					var content = await GetFileContentAsync(owner, repo, file.Path);
					
					if (IsDbContextFile(content))
					{
						Console.WriteLine($"   ? Valid DbContext: {file.Path}");
						dbContextFiles.Add(new DbContextInfo
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
			
			Console.WriteLine($"? Tree search complete: {dbContextFiles.Count} DbContext files found");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"? Tree search failed: {ex.Message}");
			throw new InvalidOperationException($"Could not search repository tree: {ex.Message}");
		}
		
		return dbContextFiles;
	}

	public async Task<string> GetFileContentAsync(string owner, string repo, string path)
	{
		try
		{
			var fileContent = await _gitHubClient.Repository.Content.GetAllContents(owner, repo, path);
			var content = fileContent.FirstOrDefault();

			if (content?.Type == ContentType.File)
			{
				return content.EncodedContent != null
					? Encoding.UTF8.GetString(Convert.FromBase64String(content.EncodedContent))
					: content.Content ?? string.Empty;
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Error getting file content from '{path}': {ex.Message}");
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
		// More robust check for DbContext files
		return content.Contains("DbContext", StringComparison.OrdinalIgnoreCase) &&
			   (content.Contains("class", StringComparison.OrdinalIgnoreCase) || 
			    content.Contains("public", StringComparison.OrdinalIgnoreCase)) &&
			   content.Contains("DbSet", StringComparison.OrdinalIgnoreCase);
	}

	// Method to validate access token
	public async Task<bool> ValidateAccessTokenAsync(string accessToken)
	{
		try
		{
			var tempClient = new GitHubClient(new ProductHeaderValue("SchemaMagic-Web"))
			{
				Credentials = new Credentials(accessToken)
			};

			var user = await tempClient.User.Current();
			return user != null;
		}
		catch
		{
			return false;
		}
	}
}

public class DbContextInfo
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
}