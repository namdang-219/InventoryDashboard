using System.Diagnostics;
using IID.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IID.Application.Common.Behaviors;

/// <summary>
/// Source name used by <see cref="PerformanceBehavior{TRequest,TResponse}"/> when
/// creating <see cref="Activity"/> instances. Must be added to the OpenTelemetry
/// tracer provider via <c>WithTracing(tracing => tracing.AddSource(PerformanceBehavior.ActivitySourceName))</c>.
/// </summary>
public static class ObservabilityDiagnostics
{
    public const string ActivitySourceName = "IID.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}

/// <summary>
/// Pipeline behavior that:
/// <list type="bullet">
///   <item>Wraps every command/query execution in an <see cref="Activity"/> (OpenTelemetry span)
///         so MediatR handlers appear as child spans under the inbound HTTP span in OpenObserve.</item>
///   <item>Logs handler execution duration via the high-performance <c>[LoggerMessage]</c>
///         source generator and emits a warning if execution exceeds the 500 ms SLA.</item>
/// </list>
/// Registered automatically by <c>ApplicationServiceCollectionExtensions.AddIidApplication</c>.
/// </summary>
public sealed partial class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    IClock clock)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestThresholdMs = 500;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Slow request {RequestType} took {ElapsedMs} ms (threshold {ThresholdMs} ms)")]
    private static partial void SlowRequest(ILogger logger, string requestType, long elapsedMs, long thresholdMs, Exception? ex);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestTypeName = typeof(TRequest).Name;

        using var activity = ObservabilityDiagnostics.ActivitySource.StartActivity(
            $"MediatR {requestTypeName}",
            ActivityKind.Internal);

        activity?.SetTag("request.type", typeof(TRequest).FullName ?? requestTypeName);
        activity?.SetTag("mediatr.pipeline", true);

        var startedAt = clock.UtcNow;

        try
        {
            var response = await next(ct).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            throw;
        }
        finally
        {
            var elapsedMs = (long)(clock.UtcNow - startedAt).TotalMilliseconds;
            activity?.SetTag("request.duration_ms", elapsedMs);

            if (elapsedMs > SlowRequestThresholdMs)
            {
                SlowRequest(logger, requestTypeName, elapsedMs, SlowRequestThresholdMs, null);
            }
        }
    }
}
