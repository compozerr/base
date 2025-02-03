namespace Api.Abstractions;

public sealed record ServerId : IdBase<ServerId>, IId<ServerId>
{
    public ServerId(Guid value) : base(value)
    {
    }

    public static ServerId Create(Guid value)
        => new(value);
}
