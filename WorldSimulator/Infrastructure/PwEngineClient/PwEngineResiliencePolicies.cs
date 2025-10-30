using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace WorldSimulator.Infrastructure.PwEngineClient;

/// <summary>
/// Polly resilience policies for PwEngine HTTP client
/// </summary>
public static class PwEngineResiliencePolicies
{
    public static ResiliencePipeline<HttpResponseMessage> CreatePipeline(
        int maxRetryAttempts = 3,
        int failuresBeforeBreaking = 5,
        int breakDurationSeconds = 30)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => !response.IsSuccessStatusCode)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    Console.WriteLine($"PwEngine retry {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = failuresBeforeBreaking,
                BreakDuration = TimeSpan.FromSeconds(breakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => !response.IsSuccessStatusCode)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnOpened = args =>
                {
                    Console.WriteLine($"PwEngine circuit breaker OPENED for {args.BreakDuration.TotalSeconds}s");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    Console.WriteLine("PwEngine circuit breaker CLOSED");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}

