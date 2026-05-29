using BlazorApp1.Commons;
using System.Net.Http.Headers;

namespace BlazorApp1.Commons.Authentication
{
    public static class GraphHttpClientService
    {

        public static IServiceCollection AddBlazorServices(this IServiceCollection services, IConfiguration configuration)
        {
            /* services.AddScoped<IValidFormJSInterop, ValidFormJSInterop>();
             services.AddScoped<ICryptoJSInterop, CryptoJSInterop>();
             services.AddScoped<IJsInteropService, JsInteropService>();
             services.AddTransient<IDomReadyJSInterop, DomReadyJSInterop>();*/
            return services;
        }
        public static IServiceCollection AddGraphHttpClientServices(this IServiceCollection services, IConfiguration configuration)
        {
            var graphSection = configuration.GetSection("Graph");
            if (graphSection == null || !graphSection.Exists())
                throw new ArgumentNullException(nameof(configuration), "No configuration for Graph");

            services.Configure<GraphOptions>(graphSection);

            services.AddHttpClient("Graph", (sp, client) =>
            {
                var graph = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(graph.Uri))
                    client.BaseAddress = new Uri(graph.Uri.TrimEnd('/') + Path.AltDirectorySeparatorChar);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.MaxRetryAttempts = 3;
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });
            return services;
        }
    }
}


