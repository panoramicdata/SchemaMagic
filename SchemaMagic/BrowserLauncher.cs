using System.Diagnostics;

namespace SchemaMagic;

internal static class BrowserLauncher
{
	public static void Open(string htmlFile)
	{
		var fullPath = Path.GetFullPath(htmlFile);
		try
		{
			if (!File.Exists(fullPath) || !string.Equals(Path.GetExtension(fullPath), ".html", StringComparison.OrdinalIgnoreCase))
			{
				throw new FileNotFoundException("Only an existing .html file can be opened.", fullPath);
			}

			// Only an escaped file URI of the validated path is ever passed as an argument
			var fileUri = new Uri(fullPath);
			if (!fileUri.IsFile)
			{
				throw new ArgumentException("Only local files can be opened.", nameof(htmlFile));
			}

			using var process = Process.Start(CreateStartInfo(fileUri.AbsoluteUri)); // nosemgrep
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️ Could not open browser: {ex.Message}");
			Console.WriteLine($"✨ Open manually: {fullPath}");
		}
	}

	private static ProcessStartInfo CreateStartInfo(string fileUri)
	{
		var startInfo = OperatingSystem.IsWindows()
			? new ProcessStartInfo("explorer.exe")
			: OperatingSystem.IsMacOS()
				? new ProcessStartInfo("open")
				: new ProcessStartInfo("xdg-open");
		startInfo.UseShellExecute = false;
		startInfo.ArgumentList.Add(fileUri);
		return startInfo;
	}
}