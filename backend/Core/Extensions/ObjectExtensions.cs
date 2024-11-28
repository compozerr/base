using System.Runtime.CompilerServices;

namespace Core.Extensions;

public static class ObjectExtensions
{
    public static T ThrowIfNull<T>(
           this T? value,
           string? paramName = null,
           bool argumentValidation = false,
           [CallerArgumentExpression(nameof(value))] string callerExpression = "",
           [CallerFilePath] string callerFilePath = "",
           [CallerLineNumber] int callerLineNumber = default)
           where T : class
           => value ?? throw CreateException("null", paramName, argumentValidation, callerExpression, callerFilePath, callerLineNumber);

    private static Exception CreateException(string violation, string? paramName, bool argumentValidation, string callerExpression, string callerFilePath, int callerLineNumber)
        => CreateException(
            violation,
            message => !paramName.IsNullOrWhiteSpace() || argumentValidation
                ? new ArgumentNullException(paramName.Clean() ?? callerExpression, message)
                : new Exception(message),
            callerExpression,
            callerFilePath,
            callerLineNumber);

    private static Exception CreateException(string violation, Func<string, Exception> exceptionFactory, string callerExpression, string callerFilePath, int callerLineNumber)
    {
        var message = $"Expression '{callerExpression}' is {violation} ({callerFilePath}:{callerLineNumber}).";

        return exceptionFactory(message);
    }

    public static string ThrowIfNullOrWhiteSpace(
       this string? value,
       string? paramName = null,
       bool argumentValidation = false,
       [CallerArgumentExpression(nameof(value))] string callerExpression = "",
       [CallerFilePath] string callerFilePath = "",
       [CallerLineNumber] int callerLineNumber = default)
       => value.Clean() ?? throw CreateException("null or white space", paramName, argumentValidation, callerExpression, callerFilePath, callerLineNumber);

}
