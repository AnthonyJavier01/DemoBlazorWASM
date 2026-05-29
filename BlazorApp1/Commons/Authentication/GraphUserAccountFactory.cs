using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace BlazorApp1.Commons.Authentication
{
    public class GraphUserAccountFactory : AccountClaimsPrincipalFactory<UserAccount>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAccessTokenProviderAccessor _accessor;
        private readonly NavigationManager _navigation;
        private readonly IJSRuntime _jsRuntime;

        private readonly Func<UserAccount, RemoteAuthenticationUserOptions, ValueTask<ClaimsPrincipal>>? _createUserAsyncOverride;

        public GraphUserAccountFactory(
            IAccessTokenProviderAccessor accessor,
            IHttpClientFactory httpClientFactory,
            NavigationManager navigation,
            IJSRuntime jsRuntime,
            Func<UserAccount, RemoteAuthenticationUserOptions, ValueTask<ClaimsPrincipal>>? createUserAsyncOverride = null
        ) : base(accessor)
        {
            _accessor = accessor;
            _httpClientFactory = httpClientFactory;
            _navigation = navigation;
            _jsRuntime = jsRuntime;
            _createUserAsyncOverride = createUserAsyncOverride;
        }
        private static async Task<string?> GetUserPhotoDataUrlAsync(HttpClient httpClient, string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("me/photo/$value");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                return $"data:image/jpeg;base64,{base64}";
            }
            return null;
        }

        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(UserAccount account, RemoteAuthenticationUserOptions options)
        {
            if (await IsTokenExpiredAsync())
            {
                await _jsRuntime.InvokeAsync<object>("sessionStorage.clear", Array.Empty<object>());
                _navigation.NavigateTo("authentication/login");
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            var user = await CreateBaseUserAsync(account, options);
            if (user.Identity is not ClaimsIdentity userIdentity)
                return user;

            AddAppRoles(userIdentity, account);

            await AddSessionClaimsAsync(userIdentity);
            await EnsureGraphClaimsAsync(userIdentity);
            return user;
        }

        private async Task<ClaimsPrincipal> CreateBaseUserAsync(UserAccount account, RemoteAuthenticationUserOptions options)
        {
            return _createUserAsyncOverride != null
                ? await _createUserAsyncOverride(account, options)
                : await base.CreateUserAsync(account, options);
        }

        private async Task AddSessionClaimsAsync(ClaimsIdentity userIdentity)
        {
            string? samValue = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", new object[] { "onPremisesSamAccountName" });
            string? photoDataUrl = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", new object[] { "user_photo_dataurl" });
            if (!string.IsNullOrWhiteSpace(samValue))
                userIdentity.AddClaim(new Claim("onPremisesSamAccountName", samValue));
            if (!string.IsNullOrWhiteSpace(photoDataUrl))
                userIdentity.AddClaim(new Claim("user_photo_dataurl", photoDataUrl));
        }

        private async Task EnsureGraphClaimsAsync(ClaimsIdentity userIdentity)
        {
            string? samValue = userIdentity.FindFirst("onPremisesSamAccountName")?.Value;
            string? photoDataUrl = userIdentity.FindFirst("user_photo_dataurl")?.Value;
            if (!string.IsNullOrWhiteSpace(samValue) && !string.IsNullOrWhiteSpace(photoDataUrl))
                return;

            var tokenResult = await _accessor.TokenProvider.RequestAccessToken();
            if (!tokenResult.TryGetToken(out var token))
                return;

            var httpClient = _httpClientFactory.CreateClient("Graph");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            bool setSessionTime = false;
            if (string.IsNullOrWhiteSpace(samValue))
            {
                samValue = await GetSamAccountNameAsync(httpClient);
                if (!string.IsNullOrWhiteSpace(samValue))
                {
                    userIdentity.AddClaim(new Claim("onPremisesSamAccountName", samValue));
                    await _jsRuntime.InvokeAsync<object>("sessionStorage.setItem", new object[] { "onPremisesSamAccountName", samValue });
                    setSessionTime = true;
                }
            }
            if (string.IsNullOrWhiteSpace(photoDataUrl))
            {
                photoDataUrl = await GetUserPhotoDataUrlAsync(httpClient, token.Value);
                if (!string.IsNullOrWhiteSpace(photoDataUrl))
                {
                    userIdentity.AddClaim(new Claim("user_photo_dataurl", photoDataUrl));
                    await _jsRuntime.InvokeAsync<object>("sessionStorage.setItem", new object[] { "user_photo_dataurl", photoDataUrl });
                    setSessionTime = true;
                }
            }
            if (setSessionTime)
            {
                var sessionTime = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", new object[] { "sessionStartTime" });
                if (string.IsNullOrWhiteSpace(sessionTime))
                {
                    await _jsRuntime.InvokeAsync<object>("sessionStorage.setItem", new object[] { "sessionStartTime", DateTime.UtcNow.ToString("o") });
                }
            }
        }

        private async Task<bool> IsTokenExpiredAsync()
        {
            var loginInfoJson = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", new object[] { "loginInfo" });
            if (string.IsNullOrWhiteSpace(loginInfoJson))
                return false;

            using (JsonDocument doc = JsonDocument.Parse(loginInfoJson))
            {
                var root = doc.RootElement;
                if (!root.TryGetProperty("Claims", out JsonElement claimsProp) || claimsProp.ValueKind != JsonValueKind.Array)
                    return false;

                var expUnix = GetExpUnixFromClaims(claimsProp);
                if (expUnix.HasValue)
                {
                    var expDate = DateTimeOffset.FromUnixTimeSeconds(expUnix.Value).UtcDateTime;
                    return DateTime.UtcNow > expDate;
                }
            }
            return false;
        }
        private static long? GetExpUnixFromClaims(JsonElement claimsProp)
        {
            foreach (JsonElement claim in claimsProp.EnumerateArray())
            {
                if (
                    claim.TryGetProperty("Type", out JsonElement typeProp) && typeProp.GetString() == "exp" &&
                    claim.TryGetProperty("Value", out JsonElement valueProp) && long.TryParse(valueProp.GetString(), out long expUnix)
                )
                {
                    return expUnix;
                }
            }
            return null;
        }

        private static void AddAppRoles(ClaimsIdentity userIdentity, UserAccount? account)
        {
            if (account?.Roles != null)
            {
                foreach (var role in account.Roles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                        userIdentity.AddClaim(new Claim("AppRole", role));
                }
            }
        }

        private static async Task<string?> GetSamAccountNameAsync(HttpClient httpClient)
        {
            var response = await httpClient.GetAsync("me?$select=onPremisesSamAccountName");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("onPremisesSamAccountName", out var sam))
                {
                    return sam.GetString();
                }
            }
            return null;
        }
    }
}




