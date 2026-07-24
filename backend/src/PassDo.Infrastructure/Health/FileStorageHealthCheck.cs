using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PassDo.Infrastructure.Options;
using PassDo.Infrastructure.Storage;

namespace PassDo.Infrastructure.Health;

public class FileStorageHealthCheck : IHealthCheck
{
    private readonly LocalFileStorageService _fileStorageService;
    private readonly FileStorageOptions _options;

    public FileStorageHealthCheck(
        LocalFileStorageService fileStorageService,
        IOptions<FileStorageOptions> options)
    {
        _fileStorageService = fileStorageService;
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rootPath = _fileStorageService.RootPath;
            Directory.CreateDirectory(rootPath);

            var probeFile = Path.Combine(rootPath, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);

            return Task.FromResult(HealthCheckResult.Healthy(
                $"File storage is accessible at '{_options.RootPath}'."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "File storage is not accessible.",
                ex));
        }
    }
}
