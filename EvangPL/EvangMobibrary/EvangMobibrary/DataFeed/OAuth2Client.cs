using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EvangSol.Mobibrary.DataFeed;

// =================================================================================
// Shared state for OAuth callback
public static class OAuthState
{
    public static TaskCompletionSource<Uri>? CallbackTcs { get; set; }
}

// =================================================================================
public interface IOAuth2Client
{
    Task<string?> AuthenticateAsync();
    Task<(string?, string?)> GetValidAccessTokenAsync(); // Auto-refresh if needed
    Task LogoutAsync();
    bool IsAuthenticated { get; }
}

public class OAuth2Client : IOAuth2Client
{
    private const string CallbackScheme = "com.evangsol.evangmes";
    private const string CallbackHost = "oauth2callback";

    public string? AccountId => LocalMemory.Account?.AccountId;
    public string? ClientId => LocalMemory.Account?.ClientId;
    public string? Scope => LocalMemory.Account?.Scope;
    public string? AuthorizeUrl => LocalMemory.Account?.OAuthUrl;
    public string? TokenUrl => LocalMemory.Account?.TokenUrl;

    private string CallbackUri => $"{CallbackScheme}://{CallbackHost}";
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string? _codeVerifier;
    private string? _state;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;

    // -------------------------------------------------------------------------
    public async Task<string?> AuthenticateAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            var (codeVerifier, codeChallenge) = GeneratePkcePair();
            _codeVerifier = codeVerifier;

            var authUrl = BuildAuthUrl(codeChallenge);
            if (authUrl == null)
                return "No client id, account id or scope available.";

            OAuthState.CallbackTcs = new();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            cts.Token.Register(() => OAuthState.CallbackTcs?.TrySetCanceled());

            await Launcher.OpenAsync(authUrl);

            Uri callback;
            try
            {
                callback = await OAuthState.CallbackTcs.Task;
            }
            catch (TaskCanceledException)
            {
                return "Authentication canceled or timed out.";
            }

            var query = ParseQuery(callback.Query);
            if (query.GetValueOrDefault("state") != _state)
                return "Invalid state parameter.";

            var code = query.GetValueOrDefault("code");
            if (string.IsNullOrEmpty(code))
                return "No authorization code received.";

            return await ExchangeCodeForTokensAsync(code);
        }
        catch (Exception ex)
        {
            return $"Login failed: {ex.Message}";
        }
        finally
        {
            _authLock.Release();
        }
    }

    // -------------------------------------------------------------------------
    public async Task<(string?, string?)> GetValidAccessTokenAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            // Load tokens if not in memory
            if (_accessToken == null || _refreshToken == null)
            {
                _accessToken = await SecureStorage.Default.GetAsync("oauth_access_token");
                _refreshToken = await SecureStorage.Default.GetAsync("oauth_refresh_token");
                if (_accessToken != null)
                {
                    // Try to read expiry (default to 1 hour if unknown)
                    var expStr = await SecureStorage.Default.GetAsync("oauth_expiry");
                    _tokenExpiry = !string.IsNullOrEmpty(expStr) && DateTime.TryParse(expStr, out var exp)
                        ? exp
                        : DateTime.UtcNow.AddHours(1);
                }
            }

            // Token is valid
            if (IsAuthenticated)
                return (_accessToken, null);

            // Try to refresh
            if (!string.IsNullOrEmpty(_refreshToken))
            {
                return await RefreshTokenAsync(_refreshToken);
            }

            // Not authenticated
            return (null, "No valid token available.");
        }
        finally
        {
            _authLock.Release();
        }
    }

    // -------------------------------------------------------------------------
    private async Task<string?> ExchangeCodeForTokensAsync(string code)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(AccountId))
            return "No client id or account id available.";

        try
        {
            using var client = new HttpClient();
            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = ClientId,
                ["code"] = code,
                ["redirect_uri"] = CallbackUri,
                ["code_verifier"] = _codeVerifier ?? string.Empty,
                ["realm"] = AccountId
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(TokenUrl, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"Token request failed: {response.StatusCode}";

            var token = JsonSerializer.Deserialize<TokenResponse>(responseText);
            if (token?.access_token == null)
                return "Invalid token response.";

            _accessToken = token.access_token;
            _refreshToken = token.refresh_token;
            int expiresin = 3600;
            //int.TryParse(token.expires_in, out expiresin);
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresin);

            // Save securely
            await SecureStorage.Default.SetAsync("oauth_access_token", _accessToken);
            await SecureStorage.Default.SetAsync("oauth_refresh_token", _refreshToken ?? string.Empty);
            await SecureStorage.Default.SetAsync("oauth_expiry", _tokenExpiry.ToString("o"));

            return null;
        }
        catch (Exception ex)
        {
            return $"Token exchange failed: {ex.Message}";
        }
    }

    // -------------------------------------------------------------------------
    private async Task<(string?, string?)> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(AccountId))
            return (null, "No client id or account id available.");

        try
        {
            using var client = new HttpClient();
            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId,
                ["refresh_token"] = refreshToken,
                ["realm"] = AccountId
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(TokenUrl, content);

            if (!response.IsSuccessStatusCode)
                return (null, "Refresh token request failed.");

            var json = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<TokenResponse>(json);
            if (token?.access_token == null)
                return (null, "Invalid refresh token response.");

            _accessToken = token.access_token;
            _refreshToken = token.refresh_token ?? refreshToken;
            int expiresin = 3600;
            //int.TryParse(token.expires_in, out expiresin);
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresin);

            await SecureStorage.Default.SetAsync("oauth_access_token", _accessToken);
            await SecureStorage.Default.SetAsync("oauth_refresh_token", _refreshToken);
            await SecureStorage.Default.SetAsync("oauth_expiry", _tokenExpiry.ToString("o"));

            return (_accessToken, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    public async Task LogoutAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            _accessToken = null;
            _refreshToken = null;
            _tokenExpiry = DateTime.MinValue;
            _codeVerifier = null;

            SecureStorage.Default.Remove("oauth_access_token");
            SecureStorage.Default.Remove("oauth_refresh_token");
            SecureStorage.Default.Remove("oauth_expiry");
        }
        finally
        {
            _authLock.Release();
        }
    }

    // -------------------------------------------------------------------------
    // PKCE
    private (string codeVerifier, string codeChallenge) GeneratePkcePair()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var codeVerifier = Base64UrlEncode(bytes);

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = Base64UrlEncode(hash);

        return (codeVerifier, codeChallenge);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // -------------------------------------------------------------------------
    // Query parser without HttpUtility
    private static Dictionary<string, string> ParseQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
            return new();

        // Remove leading '?'
        if (query.StartsWith('?'))
            query = query.Substring(1);

        return query.Split('&')
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => Uri.UnescapeDataString(pair[1]),
                StringComparer.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    private string? BuildAuthUrl(string codeChallenge)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(AccountId) || string.IsNullOrEmpty(Scope))
            return null;

        _state = Guid.NewGuid().ToString("n");

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = CallbackUri,
            ["scope"] = Scope,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = _state,
            ["realm"] = AccountId
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{AuthorizeUrl}?{queryString}";
    }
}

// =================================================================================
// Models
public class TokenResponse
{
    public string? access_token { get; set; }
    public string? refresh_token { get; set; }
    //public string? expires_in { get; set; }     // seconds
    public int expires_in { get; set; }
    public string? token_type { get; set; }
}
