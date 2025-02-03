namespace Api.Abstractions;

public sealed record LocationId : IdBase<LocationId>, IId<LocationId>
{
    public LocationId(Guid value) : base(value)
    {
    }

    public static LocationId Create(Guid value)
        => new(value);
}
