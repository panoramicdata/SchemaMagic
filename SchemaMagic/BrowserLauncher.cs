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

			using var process = Process.Start(CreateStartInfo(fullPath));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️ Could not open browser: {ex.Message}");
			Console.WriteLine($"✨ Open manually: {fullPath}");
		}
	}

	private static ProcessStartInfo CreateStartInfo(string fullPath)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = OperatingSystem.IsWindows() ? "explorer.exe" : OperatingSystem.IsMacOS() ? "open" : "xdg-open",
			UseShellExecute = false
		};
		startInfo.ArgumentList.Add(fullPath);
		return startInfo;
	}
}
