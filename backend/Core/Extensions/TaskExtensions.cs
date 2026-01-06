using System.Runtime.CompilerServices;
using Serilog;

namespace Core.Extensions;

public static class TaskExtensions
{
    public static void LogAndSilence(
        this Task @this,
        string? errorLog = null,
        object?[]? propertyValues = null,
        [CallerArgumentExpression(nameof(@this))] string expression = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
    {
        // ReSharper disable ExplicitCallerInfoArgument

        @this.LogExceptionAsError(errorLog, propertyValues, expression, callerFilePath, callerLineNumber, callerMemberName)
             .Silence();

        // ReSharper restore ExplicitCallerInfoArgument
    }

    public static Task LogAndSilenceAsync(
        this Task @this,
        string? errorLog = null,
        object?[]? propertyValues = null,
        [CallerArgumentExpression(nameof(@this))] string expression = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
        => @this.LogExceptionAsError(errorLog, propertyValues, expression, callerFilePath, callerLineNumber, callerMemberName)
                        .SilenceAsync();

    public static async Task<T> LogExceptionAsError<T>(
        this Task<T> @this,
        string? errorLog = null,
        object?[]? propertyValues = null,
        [CallerArgumentExpression(nameof(@this))] string expression = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
    {
        try
        {
            return await @this.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Logger
               .ForContext("Expression", expression)
               .ForContext("CallerFilePath", callerFilePath)
               .ForContext("CallerLineNumber", callerLineNumber)
               .ForContext("CallerMemberName", callerMemberName)
               .Error(ex, errorLog ?? "Exception occurred", propertyValues);

            throw;
        }
    }

    public static async Task LogExceptionAsError(
        this Task @this,
        string? errorLog = null,
        object?[]? propertyValues = null,
        [CallerArgumentExpression(nameof(@this))] string expression = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
    {
        try
        {
            await @this.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Logger
               .ForContext("Expression", expression)
               .ForContext("CallerFilePath", callerFilePath)
               .ForContext("CallerLineNumber", callerLineNumber)
               .ForContext("CallerMemberName", callerMemberName)
               .Error(ex, errorLog ?? "Exception occurred", propertyValues);

            throw;
        }
    }

    public static async Task<T> LogExceptionAsWarning<T>(
        this Task<T> @this,
        string? errorLog = null,
        object?[]? propertyValues = null,
        [CallerArgumentExpression(nameof(@this))] string expression = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
    {
        try
        {
            return await @this.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Logger
               .ForContext("Expression", expression)
               .ForContext("CallerFilePath", callerFilePath)
               .ForContext("CallerLineNumber", callerLineNumber)
               .ForContext("CallerMemberName", callerMemberName)
               .Warning(ex, errorLog ?? "Exception occurred", propertyValues);

            throw;
        }
    }

    public static async Task LogExceptionAsWarning(
        this Task @this,
        string? errorLog = null,
        object?[]? propertyValues = null,
        [CallerArgumentExpression(nameof(@this))] string expression = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
    {
        try
        {
            await @this.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Logger
               .ForContext("Expression", expression)
               .ForContext("CallerFilePath", callerFilePath)
               .ForContext("CallerLineNumber", callerLineNumber)
               .ForContext("CallerMemberName", callerMemberName)
               .Warning(ex, errorLog ?? "Exception occurred", propertyValues);

            throw;
        }
    }

    public static void Silence(this Task @this)
        => @this.SilenceAsync().ContinueWith(_ => { });

    public static async Task SilenceAsync(this Task @this)
    {
        try
        {
            await @this.ConfigureAwait(false);
        }
        catch
        {
            // Ignored intentionally
        }
    }

    public static async Task<T> RetryAsync<T>(
        this Task<T> @this,
        int maxRetries = 3,
        TimeSpan? delay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        var attempts = 0;
        var delayTime = delay ?? TimeSpan.FromSeconds(1);

        while (true)
        {
            try
            {
                return await @this.ConfigureAwait(false);
            }
            catch (Exception ex) when (shouldRetry?.Invoke(ex) ?? true)
            {
                if (++attempts >= maxRetries)
                    throw;

                Log.Logger
                   .ForContext("Attempts", attempts)
                   .Warning(ex, "Retrying after exception: {Message}", ex.Message);

                await Task.Delay(delayTime * (1 << attempts)).ConfigureAwait(false);
            }
        }
    }
}
