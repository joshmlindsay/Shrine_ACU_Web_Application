using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shrine_ACU_Web_Application.Models.UserRoles;
using Shrine_ACU_Web_Application.Services.Attendance;

namespace Shrine_ACU_Web_Application.Services;

public sealed class UserRolesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static UserRolesApiClient()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly HttpClient _httpClient;

    public UserRolesApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AcuCarShowApi");
    }

    public async Task<IReadOnlyList<UserRoleContextDto>> GetUserContextsAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/UserRoles/user/{userId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var contexts = await response.Content.ReadFromJsonAsync<List<UserRoleContextDto>>(JsonOptions, cancellationToken);
        return contexts ?? [];
    }

    public async Task<UserRoleContextDto?> GetContextAsync(int userId, int applicationId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/UserRoles/user/{userId}/application/{applicationId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserRoleContextDto>(JsonOptions, cancellationToken);
    }

    public async Task<UserRoleContextDto?> UpsertContextAsync(int userId, int applicationId, UserRoleUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/UserRoles/user/{userId}/application/{applicationId}", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return response.Content.Headers.ContentLength == 0
            ? null
            : await response.Content.ReadFromJsonAsync<UserRoleContextDto>(JsonOptions, cancellationToken);
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