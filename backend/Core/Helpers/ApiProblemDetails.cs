using System.Text.Json.Serialization;

namespace Core.Helpers;

/// <summary>
/// Represents a problem details object as defined by RFC 7807
/// </summary>
public class ApiProblemDetails
{
    /// <summary>
    /// A URI reference that identifies the problem type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP status code generated by the origin server for this occurrence of the problem
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    /// <summary>
    /// A unique identifier for this specific request
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Additional members carrying information about the problem
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; set; }

    /// <summary>
    /// Creates a problem details object for a 400 Bad Request response
    /// </summary>
    public static ApiProblemDetails BadRequest(string detail, string traceId, string? instance = null)
    {
        return new ApiProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = detail,
            TraceId = traceId,
            Instance = instance
        };
    }

    /// <summary>
    /// Creates a problem details object for a 401 Unauthorized response
    /// </summary>
    public static ApiProblemDetails Unauthorized(string detail, string traceId, string? instance = null)
    {
        return new ApiProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Title = "Unauthorized",
            Status = 401,
            Detail = detail,
            TraceId = traceId,
            Instance = instance
        };
    }

    /// <summary>
    /// Creates a problem details object for a 403 Forbidden response
    /// </summary>
    public static ApiProblemDetails Forbidden(string detail, string traceId, string? instance = null)
    {
        return new ApiProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Title = "Forbidden",
            Status = 403,
            Detail = detail,
            TraceId = traceId,
            Instance = instance
        };
    }

    /// <summary>
    /// Creates a problem details object for a 404 Not Found response
    /// </summary>
    public static ApiProblemDetails NotFound(string detail, string traceId, string? instance = null)
    {
        return new ApiProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = 404,
            Detail = detail,
            TraceId = traceId,
            Instance = instance
        };
    }

    /// <summary>
    /// Creates a problem details object for a 409 Conflict response
    /// </summary>
    public static ApiProblemDetails Conflict(string detail, string traceId, string? instance = null)
    {
        return new ApiProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Title = "Conflict",
            Status = 409,
            Detail = detail,
            TraceId = traceId,
            Instance = instance
        };
    }

    /// <summary>
    /// Creates a problem details object for a 500 Internal Server Error response
    /// </summary>
    public static ApiProblemDetails InternalServerError(string detail, string traceId, string? instance = null)
    {
        return new ApiProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = detail,
            TraceId = traceId,
            Instance = instance
        };
    }
}