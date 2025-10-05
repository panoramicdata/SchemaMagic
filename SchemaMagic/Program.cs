using System.CommandLine;
using System.Diagnostics;

namespace SchemaMagic;

internal class Program
{
	static async Task<int> Main(string[] args)
	{
		var dbContextFileArgument = new Argument<FileInfo?>(
			name: "dbcontext-file",
			description: "Path to the DbContext C# file")
		{
			Arity = ArgumentArity.ZeroOrOne
		};

		var outputOption = new Option<string?>(
			name: "--output",
			description: "Output HTML file path (defaults to Output/{DbContext}-Schema.html)")
		{
			IsRequired = false
		};

		var guidOption = new Option<string?>(
			name: "--guid",
			description: "Specify document GUID to preserve localStorage state (useful for bug fixes while maintaining user customizations)")
		{
			IsRequired = false
		};

		var cssFileOption = new Option<FileInfo?>(
			name: "--css-file",
			description: "Path to custom CSS file to override default styling")
		{
			IsRequired = false
		};

		var outputDefaultCssOption = new Option<string?>(
			name: "--output-default-css",
			description: "Output the default CSS file to the specified path (or 'default-schema-styles.css' if no path given)")
		{
			IsRequired = false
		};

		var rootCommand = new RootCommand("🎯 SchemaMagic - Interactive Entity Framework Core Schema Visualizer")
		{
			dbContextFileArgument,
			outputOption,
			guidOption,
			cssFileOption,
			outputDefaultCssOption
		};

		rootCommand.SetHandler(async (dbContextFile, output, guid, cssFile, outputDefaultCss) =>
		{
			try
			{
				// Handle CSS export first
				if (!string.IsNullOrEmpty(outputDefaultCss))
				{
					var cssOutputPath = string.IsNullOrEmpty(outputDefaultCss) || outputDefaultCss == "true" 
						? "default-schema-styles.css" 
						: outputDefaultCss;
					
					await SchemaGenerator.ExportDefaultCssAsync(cssOutputPath);
					Console.WriteLine($"📄 Default CSS exported to: {cssOutputPath}");
					Console.WriteLine("✨ Customize this file and use --css-file to apply your changes");
					return;
				}

				// Validate required argument for schema generation
				if (dbContextFile == null || !dbContextFile.Exists)
				{
					Console.WriteLine("❌ Error: DbContext file path is required for schema generation");
					Console.WriteLine("✨ Use --output-default-css to export default styling without generating a schema");
					return;
				}

				var htmlFile = await SchemaGenerator.GenerateSchemaAsync(dbContextFile, output, guid, cssFile);
				Console.WriteLine($"📄 Schema visualization created: {htmlFile}");

				if (!string.IsNullOrEmpty(guid))
				{
					Console.WriteLine($"🆔 Document GUID: {guid} (preserves existing localStorage state)");
				}

				if (cssFile != null)
				{
					Console.WriteLine($"🎨 Custom CSS applied from: {cssFile.FullName}");
				}

				Console.WriteLine("🌐 Opening in browser...");
				try
				{
					var psi = new ProcessStartInfo
					{
						FileName = htmlFile,
						UseShellExecute = true
					};
					Process.Start(psi);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"⚠️ Could not open browser: {ex.Message}");
					Console.WriteLine($"✨ Open manually: {htmlFile}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error: {ex.Message}");
			}
		}, dbContextFileArgument, outputOption, guidOption, cssFileOption, outputDefaultCssOption);

		return await rootCommand.InvokeAsync(args);
	}
}