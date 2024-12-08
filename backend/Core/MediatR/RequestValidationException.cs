using FluentValidation;
using FluentValidation.Results;

namespace Core.MediatR;

public class RequestValidationException(string requestName, IEnumerable<ValidationFailure> errors)
    : ValidationException($"Exception occurred validating request '{requestName}'.", errors);