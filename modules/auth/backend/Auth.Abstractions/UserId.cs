namespace Auth.Abstractions;

public sealed record UserId : IdBase<UserId>, IId<UserId>
{
    public UserId(Guid value) : base(value)
    {
    }

    public static UserId Create(Guid value)
        => new(value);
}
