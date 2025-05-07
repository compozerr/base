using System.Diagnostics;
using Core.MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Core.Helpers;

/// <summary>
/// Provides extensions for handling exceptions in a standardized way across the application
/// </summary>
public static class ExceptionHandlerExtensions
{
    /// <summary>
    /// Configures the application to use a global exception handler that returns problem details
    /// </summary>
    /// <param name="app">The web application</param>
    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
                var instance = context.Request.Path;

                ApiProblemDetails problemDetails;

                // Handle specific exception types with appropriate status codes
                switch (exception)
                {
                    case ArgumentException or ArgumentNullException or FormatException:
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        problemDetails = ApiProblemDetails.BadRequest(exception.Message, traceId, instance);
                        Log.Warning(exception, "Bad request: {Message}", exception.Message);
                        break;

                    case RequestValidationException:
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        problemDetails = ApiProblemDetails.BadRequest($"Validation errors: {string.Join(',', ((RequestValidationException)exception).Errors)}", traceId, instance);
                        Log.Warning(exception, "Validation error: {Message}", exception.Message);
                        break;

                    case UnauthorizedAccessException:
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        problemDetails = ApiProblemDetails.Unauthorized(exception.Message, traceId, instance);
                        Log.Warning(exception, "Unauthorized access: {Message}", exception.Message);
                        break;

                    case KeyNotFoundException or FileNotFoundException:
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        problemDetails = ApiProblemDetails.NotFound(exception.Message, traceId, instance);
                        Log.Warning(exception, "Resource not found: {Message}", exception.Message);
                        break;

                    case InvalidOperationException:
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                        problemDetails = ApiProblemDetails.Conflict(exception.Message, traceId, instance);
                        Log.Warning(exception, "Operation conflict: {Message}", exception.Message);
                        break;

                    case OperationCanceledException:
                        context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                        problemDetails = new ApiProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7",
                            Title = "Request Timeout",
                            Status = StatusCodes.Status408RequestTimeout,
                            Detail = exception.Message,
                            TraceId = traceId,
                            Instance = instance
                        };
                        Log.Warning(exception, "Operation canceled: {Message}", exception.Message);
                        break;

                    default:
                        // For all other exceptions, return a 500 Internal Server Error
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        string detail = exception?.Message ?? "An unexpected error occurred";

                        // In development, provide more details about the exception
                        if (app.Environment.IsDevelopment())
                        {
                            detail = exception?.ToString() ?? detail;

                            // Add stack trace as an extension property in development
                            problemDetails = ApiProblemDetails.InternalServerError(detail, traceId, instance);
                            if (exception?.StackTrace != null)
                            {
                                problemDetails.Extensions = new Dictionary<string, object>
                                {
                                    ["stackTrace"] = exception.StackTrace
                                };
                            }
                        }
                        else
                        {
                            // In production, keep details limited
                            problemDetails = ApiProblemDetails.InternalServerError(
                                "An unexpected error occurred. Please contact support if the problem persists.",
                                traceId,
                                instance);
                        }

                        Log.Error(exception, "Unhandled exception occurred: {Message}", exception?.Message);
                        break;
                }

                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });
    }
}