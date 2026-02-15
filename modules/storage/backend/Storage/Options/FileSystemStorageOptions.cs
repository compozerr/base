using System.ComponentModel.DataAnnotations;

namespace Storage.Options;

public sealed class FileSystemStorageOptions
{
    [Required]
    public required string BasePath { get; init; }
}
