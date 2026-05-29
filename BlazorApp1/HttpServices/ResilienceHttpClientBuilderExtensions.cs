
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Microsoft.Extensions.Http.Resilience;
using System.Net;
namespace BlazorApp1.HttpServices
{
    public static class ResilienceHttpClientBuilderExtensions
    {

        public static IHttpClientBuilder AddCustomResilienceHandler(this IHttpClientBuilder builder, HttpClientBase.ResilienceConfig resilienceConfig, ILogger? logger = null)
        {
            if (!resilienceConfig.Enabled)
            {
                return builder;
            }

            builder.AddResilienceHandler("AddResilience", delegate (ResiliencePipelineBuilder<HttpResponseMessage> builder2, ResilienceHandlerContext context)
            {
                builder2.AddRetry(new HttpRetryStrategyOptions
                {
                    BackoffType = DelayBackoffType.Exponential,
                    MaxRetryAttempts = resilienceConfig.MaxRetryAttempts,
                    OnRetry = delegate (OnRetryArguments<HttpResponseMessage> args)
                    {
                        logger?.LogWarning("Delaying for {TimespanTotalSeconds} seconds, then making rety {RetryAttempt}", args.RetryDelay.TotalSeconds, args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                });
                builder2.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromMilliseconds(resilienceConfig.SamplingDuration),
                    FailureRatio = resilienceConfig.FailureRatio,
                    MinimumThroughput = resilienceConfig.MinimumThroughput,
                    BreakDuration = TimeSpan.FromMilliseconds(resilienceConfig.BreakDuration),
                    ShouldHandle = delegate (CircuitBreakerPredicateArguments<HttpResponseMessage> args)
                    {
                        HttpResponseMessage result = args.Outcome.Result;
                        bool result2;
                        if (result != null)
                        {
                            HttpStatusCode statusCode = result.StatusCode;
                            if (statusCode == HttpStatusCode.RequestTimeout || statusCode == HttpStatusCode.TooManyRequests)
                            {
                                result2 = true;
                                goto IL_0030;
                            }
                        }

                        result2 = false;
                        goto IL_0030;
                    IL_0030:
                        return ValueTask.FromResult(result2);
                    },
                    OnOpened = delegate (OnCircuitOpenedArguments<HttpResponseMessage> args)
                    {
                        logger?.LogWarning("Opening the circuit for {TimespanTotalSeconds} seconds...", args.BreakDuration.TotalSeconds);
                        return default(ValueTask);
                    },
                    OnClosed = delegate
                    {
                        logger?.LogInformation("Closing the circuit...");
                        return default(ValueTask);
                    }
                });
                builder2.AddTimeout(TimeSpan.FromMilliseconds(resilienceConfig.TimeoutConnect));
            });
            return builder;
        }
    }
}

