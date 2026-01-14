using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;
using MGAO.Core.Interfaces;

namespace MGAO.GoogleCalendar;

/// <summary>
/// Authentication result with status information for GWSMO-parity account lifecycle.
/// </summary>
public enum AuthStatus
{
    Success,
    NeedsReauth,
    Blocked,
    Error
}

public class AuthResult
{
    public AuthStatus Status { get; init; }
    public UserCredential? Credential { get; init; }
    public string? ErrorMessage { get; init; }
    public bool RequiresInteraction => Status == AuthStatus.NeedsReauth || Status == AuthStatus.Blocked;
}

public class GoogleAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ITokenStore _tokenStore;

    // Request offline access to obtain refresh tokens (GWSMO-parity requirement)
    private readonly string[] _scopes = { CalendarService.Scope.Calendar };

    public GoogleAuthService(string clientId, string clientSecret, ITokenStore tokenStore)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// Authorize with interactive browser flow (for AddAccount or ReauthAccount).
    /// Forces new consent to ensure fresh refresh token.
    /// </summary>
    public async Task<UserCredential> AuthorizeInteractiveAsync(string accountId, CancellationToken ct = default)
    {
        // Use GoogleWebAuthorizationBroker which opens system browser (GWSMO-parity: no embedded browser)
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret },
            _scopes,
            accountId,
            ct);

        if (string.IsNullOrEmpty(credential.Token.RefreshToken))
        {
            throw new InvalidOperationException(
                "No refresh token received. Please revoke app access at https://myaccount.google.com/permissions and try again.");
        }

        await SaveTokenAsync(accountId, credential.Token);
        return credential;
    }

    /// <summary>
    /// Try to authorize silently using stored tokens. Returns status for UI to handle.
    /// </summary>
    public async Task<AuthResult> TryAuthorizeSilentAsync(string accountId, CancellationToken ct = default)
    {
        var existingToken = await _tokenStore.GetTokenAsync(accountId);

        if (!existingToken.HasValue || string.IsNullOrEmpty(existingToken.Value.RefreshToken))
        {
            return new AuthResult { Status = AuthStatus.NeedsReauth, ErrorMessage = "No stored credentials" };
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret },
            Scopes = _scopes,
            DataStore = new NullDataStore()
        });

        var token = new TokenResponse
        {
            AccessToken = existingToken.Value.AccessToken,
            RefreshToken = existingToken.Value.RefreshToken,
            ExpiresInSeconds = (long)(existingToken.Value.Expiry - DateTime.UtcNow).TotalSeconds
        };

        var credential = new UserCredential(flow, accountId, token);

        // Refresh if token is expired or about to expire (within 5 minutes)
        if (credential.Token.IsStale || DateTime.UtcNow >= existingToken.Value.Expiry.AddMinutes(-5))
        {
            try
            {
                if (await credential.RefreshTokenAsync(ct))
                {
                    await SaveTokenAsync(accountId, credential.Token);
                }
                else
                {
                    return new AuthResult { Status = AuthStatus.NeedsReauth, ErrorMessage = "Token refresh failed" };
                }
            }
            catch (TokenResponseException ex)
            {
                // Check for admin policy blocks or revoked access
                var error = ex.Error?.Error ?? "";
                if (error.Contains("access_denied") || error.Contains("admin_policy_enforced"))
                {
                    return new AuthResult
                    {
                        Status = AuthStatus.Blocked,
                        ErrorMessage = "Access blocked by administrator policy. Contact your Google Workspace admin."
                    };
                }
                if (error.Contains("invalid_grant"))
                {
                    return new AuthResult
                    {
                        Status = AuthStatus.NeedsReauth,
                        ErrorMessage = "Credentials expired or revoked. Please re-authenticate."
                    };
                }
                return new AuthResult { Status = AuthStatus.Error, ErrorMessage = ex.Message };
            }
        }

        return new AuthResult { Status = AuthStatus.Success, Credential = credential };
    }

    /// <summary>
    /// Legacy method for backward compatibility. Prefer TryAuthorizeSilentAsync + AuthorizeInteractiveAsync.
    /// </summary>
    public async Task<UserCredential> AuthorizeAsync(string accountId, CancellationToken ct = default)
    {
        var result = await TryAuthorizeSilentAsync(accountId, ct);

        if (result.Status == AuthStatus.Success && result.Credential != null)
        {
            return result.Credential;
        }

        // Fall back to interactive auth
        return await AuthorizeInteractiveAsync(accountId, ct);
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
