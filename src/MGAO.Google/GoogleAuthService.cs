using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;
using MGAO.Core.Interfaces;

namespace MGAO.Google;

public class GoogleAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ITokenStore _tokenStore;
    private readonly string[] _scopes = { CalendarService.Scope.Calendar };

    public GoogleAuthService(string clientId, string clientSecret, ITokenStore tokenStore)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenStore = tokenStore;
    }

    public async Task<UserCredential> AuthorizeAsync(string accountId, CancellationToken ct = default)
    {
        var existingToken = await _tokenStore.GetTokenAsync(accountId);

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret },
            Scopes = _scopes,
            DataStore = new NullDataStore()
        });

        UserCredential credential;

        if (existingToken.HasValue && !string.IsNullOrEmpty(existingToken.Value.RefreshToken))
        {
            var token = new TokenResponse
            {
                AccessToken = existingToken.Value.AccessToken,
                RefreshToken = existingToken.Value.RefreshToken,
                ExpiresInSeconds = (long)(existingToken.Value.Expiry - DateTime.UtcNow).TotalSeconds
            };

            credential = new UserCredential(flow, accountId, token);

            // Refresh if token is expired or about to expire (within 5 minutes)
            if (credential.Token.IsStale || DateTime.UtcNow >= existingToken.Value.Expiry.AddMinutes(-5))
            {
                if (await credential.RefreshTokenAsync(ct))
                {
                    await SaveTokenAsync(accountId, credential.Token);
                }
            }
        }
        else
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret },
                _scopes,
                accountId,
                ct);

            await SaveTokenAsync(accountId, credential.Token);
        }

        return credential;
    }

    public async Task<string> GetAccountEmailAsync(UserCredential credential)
    {
        var service = new Google.Apis.Oauth2.v2.Oauth2Service(
            new Google.Apis.Services.BaseClientService.Initializer { HttpClientInitializer = credential });
        var request = service.Userinfo.Get();
        var userInfo = await request.ExecuteAsync();
        return userInfo.Email ?? throw new InvalidOperationException("Could not retrieve email from Google account");
    }

    private async Task SaveTokenAsync(string accountId, TokenResponse token)
    {
        var expiry = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600);
        await _tokenStore.SaveTokenAsync(accountId, token.AccessToken, token.RefreshToken, expiry);
    }

    public async Task RevokeAsync(string accountId)
    {
        await _tokenStore.DeleteTokenAsync(accountId);
    }

    private class NullDataStore : IDataStore
    {
        public Task ClearAsync() => Task.CompletedTask;
        public Task DeleteAsync<T>(string key) => Task.CompletedTask;
        public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T));
        public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
    }
}
