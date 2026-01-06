namespace Core.Abstractions;

public abstract record IdBase<TSelf> : IParsable<TSelf>, IComparable<TSelf>
    where TSelf : IdBase<TSelf>, IId<TSelf>
{
    public Guid Value { get; }

    protected IdBase(Guid Value)
    {
        this.Value = Value;
    }

    public static TSelf CreateNew() => TSelf.Create(Guid.NewGuid());

    public static TSelf Empty => TSelf.Create(Guid.Empty);

    public sealed override string ToString()
        => Value.ToString();

    public static TSelf Parse(string s, IFormatProvider? provider = null) => TSelf.Create(Guid.Parse(s));
    public static bool TryParse(string? s, IFormatProvider? provider, out TSelf result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = TSelf.Create(guid);
            return true;
        }

        result = default!;
        return false;
    }

    public static bool TryParse(string? s, out TSelf result)
        => TryParse(s, null, out result);

    public int CompareTo(TSelf? other)
    {
        if (other is null)
            return 1;

        if (ReferenceEquals(this, other))
            return 0;

        return Value.CompareTo(other.Value);
    }

    public static bool operator ==(IdBase<TSelf>? left, TSelf? right)
        => Equals(left, right);

    public static bool operator !=(IdBase<TSelf>? left, TSelf? right)
        => !Equals(left, right);

}