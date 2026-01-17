using System.Net.Http.Json;

namespace FluxoCaixa.Blazor.Services;

public sealed class AuthService
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private readonly HttpClient _httpClient;
    private readonly LocalStorageService _storage;

    public AuthService(HttpClient httpClient, LocalStorageService storage)
    {
        _httpClient = httpClient;
        _storage = storage;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (payload is null)
        {
            return false;
        }

        await _storage.SetAsync(AccessTokenKey, payload.accessToken);
        await _storage.SetAsync(RefreshTokenKey, payload.refreshToken);
        return true;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _storage.GetAsync(AccessTokenKey);
    }

    public async Task<bool> RefreshAsync()
    {
        var refreshToken = await _storage.GetAsync(RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new { refreshToken });
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (payload is null)
        {
            return false;
        }

        await _storage.SetAsync(AccessTokenKey, payload.accessToken);
        await _storage.SetAsync(RefreshTokenKey, payload.refreshToken);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveAsync(AccessTokenKey);
        await _storage.RemoveAsync(RefreshTokenKey);
    }

    private sealed class LoginResponse
    {
        public string accessToken { get; set; } = string.Empty;
        public string refreshToken { get; set; } = string.Empty;
        public int expiresIn { get; set; }
    }
}
