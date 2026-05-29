using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Text.Json.Serialization;

namespace BlazorApp1.Commons.Authentication
{
    public class UserAccount:RemoteUserAccount
    {
        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
    }
}
