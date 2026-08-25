using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartPlace.Web.Services;

public class UserManagementApiService
{
    private readonly IHttpClientFactory
        _httpClientFactory;

    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public UserManagementApiService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory =
            httpClientFactory;

        _httpContextAccessor =
            httpContextAccessor;
    }

    // ==================================================
    // AUTHENTICATED CLIENT
    // ==================================================

    private HttpClient CreateClient()
    {
        var client =
            _httpClientFactory
                .CreateClient(
                    "SmartPlaceAPI");

        var token =
            _httpContextAccessor
                .HttpContext?
                .Session
                .GetString("JWToken");

        if (!string.IsNullOrWhiteSpace(
            token))
        {
            client.DefaultRequestHeaders
                .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        return client;
    }

    // ==================================================
    // GET USERS
    // ==================================================

    public async Task<List<AdminUserItem>>
        GetUsersAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/AdminUsers");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<AdminUserItem>>()
            ?? new();
    }

    // ==================================================
    // DELETE USER
    // ==================================================

    public async Task<ApiActionResult>
        DeleteUserAsync(
            string userId)
    {
        var client = CreateClient();

        var response =
            await client.DeleteAsync(
                $"api/AdminUsers/{userId}");

        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,

                Message =
                    "User account deleted successfully."
            };
        }

        var body =
            await response.Content
                .ReadAsStringAsync();

        return new ApiActionResult
        {
            Success = false,

            Message =
                ExtractMessage(
                    body,
                    "Unable to delete user.")
        };
    }

    // ==================================================
    // ERROR PARSER
    // ==================================================

    private static string ExtractMessage(
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
// ADMIN USER ITEM
// ==================================================

public class AdminUserItem
{
    public string UserId { get; set; } =
        string.Empty;

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public string Role { get; set; } =
        string.Empty;

    public int? StudentId { get; set; }

    public int? CompanyId { get; set; }

    public string? CompanyName
    { get; set; }

    public string? CompanyApprovalStatus
    { get; set; }

    public bool CanDelete { get; set; }

    public string? DeleteBlockedReason
    { get; set; }
}