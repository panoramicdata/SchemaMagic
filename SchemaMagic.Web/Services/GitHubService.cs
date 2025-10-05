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
			}
			catch (NotFoundException)
			{
				throw new InvalidOperationException($"Repository '{Owner}/{Name}' not found or not accessible. For private repositories, please provide a Personal Access Token.");
			}
			catch (RateLimitExceededException)
			{
				throw new InvalidOperationException("GitHub API rate limit exceeded. Please try again later or provide a Personal Access Token for higher limits.");
			}

			// Search for files containing "DbContext" in the repository
			var searchRequest = new SearchCodeRequest("DbContext")
			{
				Repos = new RepositoryCollection { $"{Owner}/{Name}" },
				Extensions = new[] { "cs" }
			};

			var searchResult = await _gitHubClient.Search.SearchCode(searchRequest);

			foreach (var file in searchResult.Items)
			{
				// Get file content to verify it's actually a DbContext
				var fileContent = await GetFileContentAsync(Owner, Name, file.Path);

				if (IsDbContextFile(fileContent))
				{
					dbContextFiles.Add(new DbContextInfo
					{
						FileName = Path.GetFileName(file.Path),
						FilePath = file.Path,
						Content = fileContent,
						Repository = $"{Owner}/{Name}"
					});
				}
			}
		}
		catch (RateLimitExceededException)
		{
			throw new InvalidOperationException("GitHub API rate limit exceeded. Please try again later or provide a Personal Access Token for higher limits.");
		}
		catch (AuthorizationException)
		{
			throw new InvalidOperationException("Access denied. This appears to be a private repository. Please provide a valid Personal Access Token with repository access.");
		}
		catch (Exception ex) when (ex.Message.Contains("API rate limit exceeded"))
		{
			throw new InvalidOperationException("GitHub API rate limit exceeded. Please try again later or provide a Personal Access Token for higher limits.");
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Error accessing GitHub repository: {ex.Message}");
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
			throw new InvalidOperationException($"Error getting file content: {ex.Message}");
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
		return content.Contains("DbContext") &&
			   (content.Contains("class") || content.Contains("public")) &&
			   content.Contains("DbSet");
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