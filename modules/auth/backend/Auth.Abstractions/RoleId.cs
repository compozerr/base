using Core.Abstractions;

namespace Auth.Abstractions;

public sealed record RoleId : IdBase<RoleId>, IId<RoleId>
{
    public RoleId(Guid value) : base(value)
    {
    }

    public static RoleId Create(Guid value)
        => new(value);
}