using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PassDo.Application.Common.Interfaces;
using PassDo.Infrastructure.Options;

namespace PassDo.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<FileStorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var absoluteFolder = Path.Combine(_rootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, storedFileName);

        await using var fileStream = File.Create(absolutePath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        var relativePath = Path.Combine(relativeFolder, storedFileName)
            .Replace('\\', '/');

        return $"{_options.RequestPath.TrimEnd('/')}/{relativePath}";
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.CompletedTask;
        }

        var relativePath = filePath;
        if (relativePath.StartsWith(_options.RequestPath, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[_options.RequestPath.Length..].TrimStart('/');
        }

        var absolutePath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public string RootPath => _rootPath;
}
