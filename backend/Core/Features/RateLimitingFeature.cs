using System.Threading.RateLimiting;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Features;

public class RateLimitingFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Set rejection status code
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global limiter - applies to all endpoints by default
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                // Get client IP address for partitioning
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for global limiter
                    });
            });

            // Add named policies for different use cases

            // 1. Strict policy - for sensitive endpoints (auth, payment, etc.)
            options.AddFixedWindowLimiter("strict", options =>
            {
                options.PermitLimit = 10;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });

            // 2. API policy - for general API endpoints
            options.AddSlidingWindowLimiter("api", options =>
            {
                options.PermitLimit = 100;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 6; // 10-second segments
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 10;
            });

            // 3. Token bucket policy - allows bursts but limits sustained rate
            options.AddTokenBucketLimiter("burst", options =>
            {
                options.TokenLimit = 50;
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                options.TokensPerPeriod = 10;
                options.AutoReplenishment = true;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 5;
            });

            // 4. Concurrency limiter - limits concurrent requests
            options.AddConcurrencyLimiter("concurrent", options =>
            {
                options.PermitLimit = 50;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 20;
            });

            // 5. Per-IP sliding window for better DDoS protection
            options.AddPolicy("per-ip", httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    });
            });

            // 6. Authenticated user policy - more lenient for authenticated users
            options.AddPolicy("authenticated", httpContext =>
            {
                var userName = httpContext.User?.Identity?.Name ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: userName,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 500,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    });
            });

            // Custom response when rate limit is exceeded
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                double? retryAfterSeconds = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = retryAfter.TotalSeconds;
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = retryAfterSeconds
                }, cancellationToken: cancellationToken);
            };
        });
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        // Enable rate limiting middleware
        app.UseRateLimiter();
    }
}
