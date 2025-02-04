namespace Api.Data;

public class Secret : BaseEntityWithId<SecretId>
{
    public string Value { get; set; } = string.Empty;

    public ServerId ServerId { get; set; } = null!;

    public virtual Server? Server { get; set; }
}