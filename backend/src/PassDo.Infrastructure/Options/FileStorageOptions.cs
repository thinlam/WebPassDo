namespace PassDo.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "uploads";
    public string RequestPath { get; set; } = "/uploads";
}
