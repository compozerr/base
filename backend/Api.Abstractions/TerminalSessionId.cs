namespace Api.Abstractions;

public sealed record TerminalSessionId : IdBase<TerminalSessionId>, IId<TerminalSessionId>
{
    public TerminalSessionId(Guid value) : base(value)
    {
    }

    public static TerminalSessionId Create(Guid value)
        => new(value);
}
