using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SchemaMagic.Core;

namespace SchemaMagic;

internal static partial class Program
{
	private static readonly JsonSerializerOptions _entitiesJsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private static async Task ProcessGitHubRepositoryAsync(string repoUrl, string? accessToken, string? outputPath, FileInfo? cssFile, bool open)
	{
		Console.WriteLine("🚀 SchemaMagic - GitHub Repository Analysis");
		Console.WriteLine("============================================================");

		var github = new GitHubService(accessToken);
		var analysisResult = await github.AnalyzeRepositoryAsync(repoUrl);

		if (analysisResult.DbContextFiles.Count == 0)
		{
			CliHelp.WriteNoDbContextHelp();
			return;
		}

		Console.WriteLine($"\n📊 Found {analysisResult.DbContextFiles.Count} DbContext file(s):");
		for (var i = 0; i < analysisResult.DbContextFiles.Count; i++)
		{
			Console.WriteLine($"   {i + 1}. {analysisResult.DbContextFiles[i].FilePath}");
		}

		Console.WriteLine($"\n📦 Retrieved {analysisResult.EntityFiles.Count} entity model file(s)");

		foreach (var dbContextFile in analysisResult.DbContextFiles)
		{
			var isFirst = analysisResult.DbContextFiles.IndexOf(dbContextFile) == 0;
			ProcessDbContextFile(repoUrl, dbContextFile, analysisResult.EntityFiles, outputPath, cssFile, open && isFirst);
		}

		Console.WriteLine($"\n✅ Successfully processed {analysisResult.DbContextFiles.Count} DbContext file(s)");
		Console.WriteLine($"📂 All output saved to: {Path.GetFullPath("Output")}");
	}

	private static void ProcessDbContextFile(
		string repoUrl,
		DbContextFileInfo dbContextFile,
		Dictionary<string, string> entityFiles,
		string? outputPath,
		FileInfo? cssFile,
		bool open)
	{
		Console.WriteLine($"\n🔍 Analyzing: {dbContextFile.FileName}");

		var documentGuid = GenerateGuidFromRepoAndFile(repoUrl, dbContextFile.FilePath);
		Console.WriteLine($"🔑 Generated GUID: {documentGuid} (preserves layout state)");

		var result = CoreSchemaAnalysisService.AnalyzeDbContextWithEntityFiles(
			dbContextFile.Content,
			dbContextFile.FileName,
			entityFiles,
			documentGuid);

		if (!result.Success)
		{
			Console.WriteLine($"❌ Analysis failed: {result.ErrorMessage}");
			return;
		}

		Console.WriteLine($"✅ Found {result.EntitiesFound} entities");
		WriteEntitySummary(result.Entities!);

		var outputFile = outputPath ?? Path.Combine("Output", $"{Path.GetFileNameWithoutExtension(dbContextFile.FileName)}-Schema.html");
		var outputDir = Path.GetDirectoryName(outputFile);
		if (!string.IsNullOrEmpty(outputDir))
		{
			Directory.CreateDirectory(outputDir);
		}

		File.WriteAllText(outputFile, BuildHtml(result, documentGuid, cssFile));

		Console.WriteLine($"📄 Schema saved to: {Path.GetFullPath(outputFile)}");
		Console.WriteLine($"📏 File size: {new FileInfo(outputFile).Length / 1024:N0} KB");
		Console.WriteLine($"🔗 Repository: {dbContextFile.Repository}");
		Console.WriteLine($"📁 Source: {dbContextFile.FilePath}");

		if (open)
		{
			Console.WriteLine("\n🌐 Opening in browser...");
			BrowserLauncher.Open(outputFile);
		}
	}

	private static void WriteEntitySummary(Dictionary<string, EntityInfo> entities)
	{
		foreach (var entity in entities.Take(5))
		{
			Console.WriteLine($"   📋 {entity.Key}: {entity.Value.Properties.Count} properties");
		}

		if (entities.Count > 5)
		{
			Console.WriteLine($"   ... and {entities.Count - 5} more entities");
		}
	}

	private static string BuildHtml(SchemaAnalysisResult result, string documentGuid, FileInfo? cssFile)
	{
		if (cssFile is null || !cssFile.Exists)
		{
			return result.HtmlContent;
		}

		var customCss = File.ReadAllText(cssFile.FullName);
		var entitiesJson = JsonSerializer.Serialize(result.Entities, _entitiesJsonOptions);
		Console.WriteLine($"🎨 Applied custom CSS from: {cssFile.FullName}");
		return ModularHtmlTemplate.GenerateWithCustomCss(entitiesJson, documentGuid, customCss);
	}

	private static string GenerateGuidFromRepoAndFile(string repoUrl, string filePath)
	{
		var combined = $"{repoUrl.Trim().ToLowerInvariant()}|{filePath.Trim().ToLowerInvariant()}";
		var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));

		var guidBytes = new byte[16];
		Array.Copy(hashBytes, guidBytes, 16);

		guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x40);
		guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

		return new Guid(guidBytes).ToString();
	}
}
