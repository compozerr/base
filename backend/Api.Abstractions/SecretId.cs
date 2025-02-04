namespace Api.Abstractions;

public sealed record SecretId : IdBase<SecretId>, IId<SecretId>
{
    public SecretId(Guid value) : base(value)
    {
    }

    public static SecretId Create(Guid value)
        => new(value);
}
