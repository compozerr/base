using MediatR;

namespace Core.MediatR;

public interface ICommand : IRequest;
public interface ICommand<out TResult> : IRequest<TResult>;