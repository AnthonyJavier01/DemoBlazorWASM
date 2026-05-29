using System.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorApp1.HttpServices
{
    public static class HttpClientExtension
    {

        //
        // Resumen:
        //     Registra los clientes HTTP externos y retorna un diccionario de IHttpClientBuilder
        //     por nombre de cliente. Esto permite que capas superiores agreguen handlers adicionales
        //     sin duplicar la llamada a AddHttpClient.
        public static IDictionary<string, IHttpClientBuilder> AddExternalHttpClientBuilders(this IServiceCollection services, IConfiguration configuration, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(services, "services");
            ArgumentNullException.ThrowIfNull(configuration, "configuration");
            HttpClientOptions httpClientsOptions = new HttpClientOptions();
            configuration.GetSection("Http").Bind(httpClientsOptions);
            Dictionary<string, IHttpClientBuilder> dictionary = new Dictionary<string, IHttpClientBuilder>();
            foreach (KeyValuePair<string, HttpClientOptions.ClientHttp> client in httpClientsOptions.Clients)
            {
                client.Deconstruct(out var key, out var value);
                string text = key;
                HttpClientOptions.ClientHttp httpClient = value;
                IHttpClientBuilder value2 = services.AddHttpClient(text, delegate (HttpClient client)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(httpClient.BaseUrl, "BaseUrl");
                    client.BaseAddress = new Uri(httpClient.BaseUrl);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    foreach (KeyValuePair<string, string> header in httpClient.Headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }).AddCustomResilienceHandler(httpClient.Resilience, logger);
                dictionary[text] = value2;
            }

            return dictionary;
        }
    


}
}



