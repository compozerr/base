namespace Auth.Abstractions;

public sealed record ApiTokenId : IdBase<ApiTokenId>, IId<ApiTokenId>
{
    public ApiTokenId(Guid value) : base(value)
    {
    }

    public static ApiTokenId Create(Guid value)
        => new(value);
}
