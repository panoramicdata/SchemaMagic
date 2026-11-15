namespace SchemaMagic.Tests;

/// <summary>
/// A throwaway directory tree that is deleted when disposed.
/// </summary>
public sealed class TempDirectory : IDisposable
{
	public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "schemamagic-tests-" + Guid.NewGuid().ToString("N"));

	public TempDirectory() => Directory.CreateDirectory(Path);

	public string Write(string relativePath, string content)
	{
		var fullPath = System.IO.Path.Combine(Path, relativePath);
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
		File.WriteAllText(fullPath, content);
		return fullPath;
	}

	public void Dispose()
	{
		if (Directory.Exists(Path))
		{
			Directory.Delete(Path, recursive: true);
		}
	}
}
