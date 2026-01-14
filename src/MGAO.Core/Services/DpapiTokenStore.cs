using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MGAO.Core.Interfaces;

namespace MGAO.Core.Services;

public class DpapiTokenStore : ITokenStore
{
    private readonly string _storagePath;

    public DpapiTokenStore(string? storagePath = null)
    {
        _storagePath = storagePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MGAO", "tokens");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task SaveTokenAsync(string accountId, string accessToken, string refreshToken, DateTime expiry)
    {
        var data = new TokenData(accessToken, refreshToken, expiry);
        var json = JsonSerializer.Serialize(data);
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(json),
            null,
            DataProtectionScope.CurrentUser);

        var filePath = GetFilePath(accountId);
        await File.WriteAllBytesAsync(filePath, encrypted);
    }

    public async Task<(string? AccessToken, string? RefreshToken, DateTime Expiry)?> GetTokenAsync(string accountId)
    {
        var filePath = GetFilePath(accountId);
        if (!File.Exists(filePath)) return null;

        try
        {
            var encrypted = await File.ReadAllBytesAsync(filePath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            var data = JsonSerializer.Deserialize<TokenData>(json);

            return data is null ? null : (data.AccessToken, data.RefreshToken, data.Expiry);
        }
        catch (CryptographicException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token decryption failed for {accountId}: {ex.Message}");
            // Token file is corrupted or from different user - delete and return null
            try { File.Delete(filePath); } catch { /* Ignore delete errors */ }
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token JSON parse failed for {accountId}: {ex.Message}");
            try { File.Delete(filePath); } catch { /* Ignore delete errors */ }
            return null;
        }
    }

    public Task DeleteTokenAsync(string accountId)
    {
        var filePath = GetFilePath(accountId);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAllAccountIdsAsync()
    {
        var files = Directory.GetFiles(_storagePath, "*.token");
        var ids = files.Select(f => Path.GetFileNameWithoutExtension(f));
        return Task.FromResult(ids);
    }

    private string GetFilePath(string accountId) =>
        Path.Combine(_storagePath, $"{SanitizeAccountId(accountId)}.token");

    private static string SanitizeAccountId(string accountId) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(accountId)).Replace('/', '_').Replace('+', '-');

    private record TokenData(string AccessToken, string RefreshToken, DateTime Expiry);
}
