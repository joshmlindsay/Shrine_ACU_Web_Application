using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shrine_ACU_Web_Application.Models.UserRoles;
using Shrine_ACU_Web_Application.Services.Attendance;

namespace Shrine_ACU_Web_Application.Services;

public sealed class UserEventRolesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static UserEventRolesApiClient()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly HttpClient _httpClient;

    public UserEventRolesApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AcuCarShowApi");
    }

    public async Task<IReadOnlyList<UserEventRoleContextDto>> GetUserEventContextsAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/UserEventRoles/user/{userId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var contexts = await response.Content.ReadFromJsonAsync<List<UserEventRoleContextDto>>(JsonOptions, cancellationToken);
        return contexts ?? [];
    }

    public async Task<UserEventRoleContextDto?> GetContextAsync(int userId, int eventId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/UserEventRoles/user/{userId}/event/{eventId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserEventRoleContextDto>(JsonOptions, cancellationToken);
    }

    public async Task<UserEventRoleContextDto?> UpsertContextAsync(int userId, int eventId, UserEventRoleUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/UserEventRoles/user/{userId}/event/{eventId}", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return response.Content.Headers.ContentLength == 0
            ? null
            : await response.Content.ReadFromJsonAsync<UserEventRoleContextDto>(JsonOptions, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ApiErrorException(new ApiError
        {
            StatusCode = (int)response.StatusCode,
            Message = string.IsNullOrWhiteSpace(rawBody) ? response.ReasonPhrase ?? "Unexpected API error." : rawBody,
            RawBody = rawBody
        });
    }
}
