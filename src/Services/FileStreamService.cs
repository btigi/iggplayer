namespace IggPlayer.Services;

public class FileStreamService
{
    private readonly string _basePath;

    public FileStreamService(IConfiguration configuration)
    {
        _basePath = configuration["MusicFilesBasePath"]
            ?? throw new InvalidOperationException("MusicFilesBasePath is not configured in appsettings.json");
    }

    public string ResolvePath(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        if (!full.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected.");

        return full;
    }

    public FileStream OpenRead(string relativePath)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("MP3 file not found.", fullPath);

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
    }

    public long GetFileSize(string relativePath)
    {
        var fullPath = ResolvePath(relativePath);
        return new FileInfo(fullPath).Length;
    }
}