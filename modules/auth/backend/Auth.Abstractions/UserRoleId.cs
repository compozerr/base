using Core.Abstractions;

namespace Auth.Abstractions;

public sealed record UserRoleId : IdBase<UserRoleId>, IId<UserRoleId>
{
    public UserRoleId(Guid value) : base(value)
    {
    }

    public static UserRoleId Create(Guid value)
        => new(value);
}