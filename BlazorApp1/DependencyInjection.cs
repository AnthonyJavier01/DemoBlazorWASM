using BlazorApp1.Commons;
using BlazorApp1.Commons.Authentication;
using BlazorApp1.HttpServices;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BlazorApp1
{
    public static class DependencyInjection
    {
        private static readonly string[] DefaultGraphScopes = ["User.Read"];
        public static void AddWebDependencies(this IServiceCollection services, IConfiguration configuration, ILogger? logger = null)
        {
           
            // Automatically registers all external HTTP clients defined in the Http:Clients section of appsettings.*.json
            services.AddExternalHttpClientBuilders(configuration, logger);
        }
        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMsalAuthentication<RemoteAuthenticationState, UserAccount>(options =>
            {
                var graphOptions = configuration.GetSection("Graph").Get<GraphOptions>() ?? new GraphOptions();
                var scopes = (graphOptions.Scopes != null && graphOptions.Scopes.Length > 0) ? graphOptions.Scopes : DefaultGraphScopes;
                foreach (var scope in scopes)
                {
                    options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
                }
                configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            }).AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, UserAccount, GraphUserAccountFactory>();
        }
    }
}



