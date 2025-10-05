namespace SchemaMagic.Web.Services;

public class GitHubService(HttpClient httpClient)
{
	private readonly GitHubClient _gitHubClient = new GitHubClient(new ProductHeaderValue("SchemaMagic-Web"));

	public async Task<List<DbContextInfo>> FindDbContextFilesAsync(string repoUrl)
	{
		var repoInfo = ParseGitHubUrl(repoUrl) ?? throw new ArgumentException("Invalid GitHub repository URL");
		var dbContextFiles = new List<DbContextInfo>();

		try
		{
			// Search for files containing "DbContext" in the repository
			var searchRequest = new SearchCodeRequest("DbContext")
			{
				Repos = new RepositoryCollection { $"{repoInfo.Owner}/{repoInfo.Name}" },
				Extensions = new[] { "cs" }
			};

			var searchResult = await _gitHubClient.Search.SearchCode(searchRequest);

			foreach (var file in searchResult.Items)
			{
				// Get file content to verify it's actually a DbContext
				var fileContent = await GetFileContentAsync(repoInfo.Owner, repoInfo.Name, file.Path);

				if (IsDbContextFile(fileContent))
				{
					dbContextFiles.Add(new DbContextInfo
					{
						FileName = Path.GetFileName(file.Path),
						FilePath = file.Path,
						Content = fileContent,
						Repository = $"{repoInfo.Owner}/{repoInfo.Name}"
					});
				}
			}
		}
		catch (RateLimitExceededException)
		{
			throw new InvalidOperationException("GitHub API rate limit exceeded. Please try again later.");
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
}

public class DbContextInfo
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
}