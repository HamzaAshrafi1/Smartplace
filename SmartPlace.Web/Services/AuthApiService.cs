using System.Net.Http.Json;
using System.Text.Json;

namespace SmartPlace.Web.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory
        _httpClientFactory;

    public AuthApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory =
            httpClientFactory;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory
            .CreateClient(
                "SmartPlaceAPI");
    }

    // ==================================================
    // LOGIN
    // ==================================================

    public async Task<AuthLoginResult>
        LoginAsync(
            LoginRequest request)
    {
        var client =
            CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Auth/login",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var body =
                await response.Content
                    .ReadAsStringAsync();

            return new AuthLoginResult
            {
                Success = false,

                Message =
                    ExtractApiMessage(
                        body,
                        "Invalid email or password.")
            };
        }

        var loginResponse =
            await response.Content
                .ReadFromJsonAsync<
                    LoginResponse>();

        if (loginResponse == null ||
            string.IsNullOrWhiteSpace(
                loginResponse.Token))
        {
            return new AuthLoginResult
            {
                Success = false,

                Message =
                    "The login response was invalid."
            };
        }

        return new AuthLoginResult
        {
            Success = true,

            Message =
                "Login successful.",

            Response =
                loginResponse
        };
    }

    // ==================================================
    // REGISTER
    // ==================================================

    public async Task<ApiActionResult>
        RegisterAsync(
            RegisterRequest request)
    {
        var client =
            CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Auth/register",
                request);

        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,

                Message =
                    "Registration successful."
            };
        }

        var body =
            await response.Content
                .ReadAsStringAsync();

        return new ApiActionResult
        {
            Success = false,

            Message =
                ExtractApiMessage(
                    body,
                    "Registration failed.")
        };
    }

    // ==================================================
    // ERROR MESSAGE
    // ==================================================

    private static string ExtractApiMessage(
        string body,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(
            body))
        {
            return fallback;
        }

        try
        {
            using var document =
                JsonDocument.Parse(body);

            if (document.RootElement
                .TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString()
                    ?? fallback;
            }
        }
        catch
        {
        }

        return fallback;
    }
}


// ==================================================
// REQUEST MODELS
// ==================================================

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


// ==================================================
// LOGIN RESPONSE
// ==================================================

public class LoginResponse
{
    public string Token { get; set; } =
        string.Empty;

    public DateTime ExpiresAt { get; set; }

    public AuthenticatedUser User
    { get; set; } =
        new();
}

public class AuthenticatedUser
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

public class AuthLoginResult
{
    public bool Success { get; set; }

    public string Message { get; set; } =
        string.Empty;

    public LoginResponse? Response
    { get; set; }
}