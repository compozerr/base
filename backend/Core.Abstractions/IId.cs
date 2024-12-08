namespace Core.Abstractions;

public interface IId<out TSelf> where TSelf : IdBase<TSelf>, IId<TSelf>, IParsable<TSelf>, IComparable<TSelf>
{
    public static abstract TSelf Create(Guid value);
}