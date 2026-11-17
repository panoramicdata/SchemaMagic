using Octokit;
using SchemaMagic.Core;
using System.Text;

namespace SchemaMagic.Web.Services;

public class GitHubService
{
	private const string RateLimitMessage = "GitHub API rate limit exceeded. Please provide a Personal Access Token for higher limits, or try again later.";

	private readonly GitHubClient _gitHubClient = new(new ProductHeaderValue("SchemaMagic-Web"));

	// GitHub Personal Access Token for private repositories
	public void SetAccessToken(string accessToken)
	{
		if (!string.IsNullOrEmpty(accessToken))
		{
			_gitHubClient.Credentials = new Credentials(accessToken);
		}
	}

	public async Task<List<DbContextInfo>> FindDbContextFilesAsync(string repoUrl, string? accessToken)
	{
		var (owner, name) = GitHubSourceHelpers.ParseRepositoryUrl(repoUrl)
			?? throw new ArgumentException("Invalid GitHub repository URL");

		if (!string.IsNullOrEmpty(accessToken))
		{
			SetAccessToken(accessToken);
		}

		try
		{
			await EnsureRepositoryAccessibleAsync(owner, name, accessToken);

			// Code search is faster but needs authentication; the tree search works for public repositories
			var found = string.IsNullOrEmpty(accessToken) ? null : await TryCodeSearchAsync(owner, name);
			return found ?? await FallbackTreeSearch(owner, name);
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			throw new InvalidOperationException(DescribeAccessFailure(ex));
		}
	}

	private static string DescribeAccessFailure(Exception ex) => ex switch
	{
		RateLimitExceededException => RateLimitMessage,
		AuthorizationException => "Access denied. This appears to be a private repository. Please provide a valid Personal Access Token with repository read permissions.",
		_ when ex.Message.Contains("API rate limit exceeded") => RateLimitMessage,
		_ => $"Error accessing GitHub repository: {ex.Message}"
	};

	public async Task<string> GetFileContentAsync(string owner, string repo, string path)
	{
		try
		{
			var fileContent = await _gitHubClient.Repository.Content.GetAllContents(owner, repo, path);
			var content = fileContent.FirstOrDefault();

			if (content?.Type == ContentType.File)
			{
				return content.EncodedContent is not null
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

	public async Task<bool> ValidateAccessTokenAsync(string accessToken)
	{
		try
		{
			var tempClient = new GitHubClient(new ProductHeaderValue("SchemaMagic-Web"))
			{
				Credentials = new Credentials(accessToken)
			};

			return await tempClient.User.Current() is not null;
		}
		catch
		{
			return false;
		}
	}

	private async Task EnsureRepositoryAccessibleAsync(string owner, string name, string? accessToken)
	{
		try
		{
			var repository = await _gitHubClient.Repository.Get(owner, name);

			if (repository.Private && string.IsNullOrEmpty(accessToken))
			{
				throw new InvalidOperationException($"Repository '{owner}/{name}' is private. Please provide a Personal Access Token with repository read permissions.");
			}
		}
		catch (NotFoundException)
		{
			throw new InvalidOperationException($"Repository '{owner}/{name}' does not exist or you don't have permission to access it. Please check the URL and try again.");
		}
		catch (RateLimitExceededException)
		{
			throw new InvalidOperationException(RateLimitMessage);
		}
		catch (AuthorizationException)
		{
			throw new InvalidOperationException("Authorization failed. If this is a private repository, please provide a valid Personal Access Token with repository read permissions.");
		}
	}

	// Returns null when the search cannot be used or found nothing, so the caller falls back
	private async Task<List<DbContextInfo>?> TryCodeSearchAsync(string owner, string name)
	{
		try
		{
			var searchRequest = new SearchCodeRequest("DbContext")
			{
				Repos = new RepositoryCollection { $"{owner}/{name}" },
				Extensions = ["cs"]
			};
			var searchResult = await _gitHubClient.Search.SearchCode(searchRequest);

			var dbContextFiles = new List<DbContextInfo>();
			foreach (var file in searchResult.Items)
			{
				var info = await TryReadDbContextAsync(owner, name, file.Path);
				if (info is not null)
				{
					dbContextFiles.Add(info);
				}
			}

			return dbContextFiles.Count > 0 || searchResult.TotalCount == 0 ? dbContextFiles : null;
		}
		catch (ApiValidationException)
		{
			Console.WriteLine("Code search requires auth - falling back to tree search");
			return null;
		}
	}

	private async Task<List<DbContextInfo>> FallbackTreeSearch(string owner, string repo)
	{
		try
		{
			var repository = await _gitHubClient.Repository.Get(owner, repo);
			var tree = await _gitHubClient.Git.Tree.GetRecursive(owner, repo, repository.DefaultBranch);

			var dbContextFiles = new List<DbContextInfo>();
			var candidates = tree.Tree
				.Where(item => item.Type == TreeType.Blob && GitHubSourceHelpers.IsDbContextCandidatePath(item.Path));

			foreach (var file in candidates)
			{
				try
				{
					var info = await TryReadDbContextAsync(owner, repo, file.Path);
					if (info is not null)
					{
						dbContextFiles.Add(info);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error reading {file.Path}: {ex.Message}");
				}
			}

			return dbContextFiles;
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Could not search repository tree: {ex.Message}");
		}
	}

	private async Task<DbContextInfo?> TryReadDbContextAsync(string owner, string repo, string path)
	{
		var content = await GetFileContentAsync(owner, repo, path);
		if (!GitHubSourceHelpers.IsDbContextFile(content))
		{
			return null;
		}

		return new DbContextInfo
		{
			FileName = Path.GetFileName(path),
			FilePath = path,
			Content = content,
			Repository = $"{owner}/{repo}"
		};
	}
}

public class DbContextInfo
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
}
