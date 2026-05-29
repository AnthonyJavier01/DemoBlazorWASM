namespace BlazorApp1.HttpServices
{
    public class HttpClientOptions
    {
        public class ClientHttp : HttpClientBase
        {
            public List<string> Scopes { get; set; } = new List<string>();
        }

        public const string HttpClients = "Http";

        public Dictionary<string, ClientHttp> Clients { get; set; } = new Dictionary<string, ClientHttp>();
    }
}

