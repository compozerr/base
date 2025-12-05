using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class Secret : BaseEntityWithId<SecretId>
{
    [MaxLength(5000)]
    public string Value { get; set; } = string.Empty;

    public ServerId ServerId { get; set; } = null!;

    public virtual Server? Server { get; set; }
}