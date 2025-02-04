namespace Api.Data;

public class Secret : BaseEntityWithId<SecretId>
{
    public string Value { get; set; }

    public ServerId ServerId { get; set; }

    public virtual Server? Server { get; set; }
}