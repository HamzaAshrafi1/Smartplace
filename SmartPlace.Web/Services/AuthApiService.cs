using System.Net.Http.Json;

namespace SmartPlace.Web.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // --------------------------------------------------
    // LOGIN
    // --------------------------------------------------

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request)
    {
        var client =
            _httpClientFactory.CreateClient(
                "SmartPlaceAPI");

        var response = await client.PostAsJsonAsync(
            "api/Auth/login",
            request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<LoginResponse>();
    }

    // --------------------------------------------------
    // REGISTER
    // --------------------------------------------------

    public async Task<bool> RegisterAsync(
        RegisterRequest request)
    {
        var client =
            _httpClientFactory.CreateClient(
                "SmartPlaceAPI");

        var response = await client.PostAsJsonAsync(
            "api/Auth/register",
            request);

        return response.IsSuccessStatusCode;
    }
}


// --------------------------------------------------
// REQUEST / RESPONSE MODELS
// --------------------------------------------------

public class LoginRequest
{
    public string Email { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;
}

public class RegisterRequest
{
    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;

    public string Role { get; set; } =
        string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } =
        string.Empty;

    public DateTime ExpiresAt { get; set; }

    public LoginUser User { get; set; } =
        new();
}

public class LoginUser
{
    public string Id { get; set; } =
        string.Empty;

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public List<string> Roles { get; set; } =
        new();
}