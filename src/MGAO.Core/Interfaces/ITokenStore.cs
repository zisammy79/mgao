namespace MGAO.Core.Interfaces;

public interface ITokenStore
{
    Task SaveTokenAsync(string accountId, string accessToken, string refreshToken, DateTime expiry);
    Task<(string? AccessToken, string? RefreshToken, DateTime Expiry)?> GetTokenAsync(string accountId);
    Task DeleteTokenAsync(string accountId);
    Task<IEnumerable<string>> GetAllAccountIdsAsync();
}
