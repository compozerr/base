namespace Auth.Abstractions;

public sealed record UserLoginId : IdBase<UserLoginId>, IId<UserLoginId>
{
    public UserLoginId(Guid value) : base(value)
    {
    }

    public static UserLoginId Create(Guid value)
        => new(value);
}