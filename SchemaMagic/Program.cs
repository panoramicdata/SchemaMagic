using System.CommandLine;

namespace SchemaMagic;

internal static partial class Program
{
	private static readonly Argument<FileInfo?> _dbContextFileArgument = new("dbcontext-file")
	{
		Description = "Path to the DbContext C# file (local file analysis)",
		Arity = ArgumentArity.ZeroOrOne
	};

	private static readonly Option<string?> _githubRepoOption = new("--github-repo")
	{
		Description = "GitHub repository URL to analyze (e.g., https://github.com/panoramicdata/SchemaMagic)"
	};

	private static readonly Option<string?> _githubTokenOption = new("--github-token")
	{
		Description = "GitHub Personal Access Token for private repositories or higher rate limits"
	};

	private static readonly Option<string?> _outputOption = new("--output")
	{
		Description = "Output HTML file path (default: Output/{DbContext}-Schema.html)"
	};

	private static readonly Option<string?> _guidOption = new("--guid")
	{
		Description = "Custom document GUID to preserve localStorage state (advanced)"
	};

	private static readonly Option<FileInfo?> _cssFileOption = new("--css-file")
	{
		Description = "Path to custom CSS file for styling customization"
	};

	private static readonly Option<string?> _outputDefaultCssOption = new("--output-default-css")
	{
		Description = "Export default CSS to file for customization (e.g., --output-default-css styles.css)"
	};

	private static readonly Option<bool> _openOption = new("--open")
	{
		Description = "Open the generated HTML in the default browser after generation (off by default)"
	};

	private static async Task<int> Main(string[] args)
	{
		var rootCommand = new RootCommand("🎯 SchemaMagic - Interactive Entity Framework Core Schema Visualizer")
		{
			_dbContextFileArgument,
			_githubRepoOption,
			_githubTokenOption,
			_outputOption,
			_guidOption,
			_cssFileOption,
			_outputDefaultCssOption,
			_openOption
		};

		rootCommand.Description = CliHelp.Description;
		rootCommand.SetAction(async (parseResult, _) => await RunAsync(parseResult));

		return await rootCommand.Parse(args).InvokeAsync();
	}

	private static async Task<int> RunAsync(ParseResult parseResult)
	{
		try
		{
			var outputDefaultCss = parseResult.GetValue(_outputDefaultCssOption);
			if (!string.IsNullOrEmpty(outputDefaultCss))
			{
				return await ExportDefaultCssAsync(outputDefaultCss);
			}

			var githubRepo = parseResult.GetValue(_githubRepoOption);
			if (!string.IsNullOrEmpty(githubRepo))
			{
				await ProcessGitHubRepositoryAsync(
					githubRepo,
					parseResult.GetValue(_githubTokenOption),
					parseResult.GetValue(_outputOption),
					parseResult.GetValue(_cssFileOption),
					parseResult.GetValue(_openOption));
				return 0;
			}

			return await GenerateFromLocalFileAsync(parseResult);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"❌ Error: {ex.Message}");
			return 1;
		}
	}

	private static async Task<int> ExportDefaultCssAsync(string cssOutputPath)
	{
		var path = cssOutputPath == "true" ? "default-schema-styles.css" : cssOutputPath;
		await SchemaGenerator.ExportDefaultCssAsync(path);
		Console.WriteLine($"📄 Default CSS exported to: {path}");
		Console.WriteLine("✨ Customize this file and use --css-file to apply your changes");
		return 0;
	}

	private static async Task<int> GenerateFromLocalFileAsync(ParseResult parseResult)
	{
		var dbContextFile = parseResult.GetValue(_dbContextFileArgument);
		if (dbContextFile is null || !dbContextFile.Exists)
		{
			CliHelp.WriteMissingArgumentHelp();
			return 1;
		}

		var cssFile = parseResult.GetValue(_cssFileOption);
		var guid = parseResult.GetValue(_guidOption);
		var htmlFile = await SchemaGenerator.GenerateSchemaAsync(
			dbContextFile,
			parseResult.GetValue(_outputOption),
			guid,
			cssFile);
		Console.WriteLine($"📄 Schema visualization created: {htmlFile}");

		if (!string.IsNullOrEmpty(guid))
		{
			Console.WriteLine($"🆔 Document GUID: {guid} (preserves existing localStorage state)");
		}

		if (cssFile is not null)
		{
			Console.WriteLine($"🎨 Custom CSS applied from: {cssFile.FullName}");
		}

		if (parseResult.GetValue(_openOption))
		{
			Console.WriteLine("🌐 Opening in browser...");
			BrowserLauncher.Open(htmlFile);
		}
		else
		{
			Console.WriteLine($"✨ Open manually: {htmlFile}");
		}

		return 0;
	}
}
