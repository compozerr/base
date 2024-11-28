using System.Collections.Immutable;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.MediatR;

public class ValidationPipelineBehavior<TRequest, TResponse>(
    ILogger<ValidationPipelineBehavior<TRequest, TResponse>> logger,
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IReadOnlyList<IValidator<TRequest>> _validators = (validators ?? throw new ArgumentNullException(nameof(validators))).ToImmutableList();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(request, cancellationToken)));
        var errors = results.SelectMany(result => result.Errors)
                            .Where(error => error != null)
                            .ToList();

        if (errors.Count > 0)
        {
            _logger.LogDebug(string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage)));
            throw new RequestValidationException(typeof(TRequest).Name, errors);
        }

        return await next();
    }
}