using System.Text.Json;
using Shrine_ACU_Web_Application.Models.Attendance;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class AttendanceEventsApiClient(IHttpClientFactory httpClientFactory, ILogger<AttendanceEventsApiClient> logger)
    : AttendanceApiClientBase(httpClientFactory, logger)
{
    public Task<List<AttendanceEventDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetEventListAsync("/api/AttendanceEvents", cancellationToken);

    public Task<PagedResult<AttendanceEventDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync<AttendanceEventDto>($"/api/AttendanceEvents/paged?page={page}&pageSize={pageSize}", page, pageSize, cancellationToken);

    public Task<List<AttendanceEventDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        GetEventListAsync("/api/AttendanceEvents/active", cancellationToken);

    public Task<AttendanceEventDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<AttendanceEventDto>($"/api/AttendanceEvents/{id}", cancellationToken);

    public Task<AttendanceEventDto?> CreateAsync(AttendanceEventDto dto, CancellationToken cancellationToken = default) =>
        PostAsync<AttendanceEventDto>("/api/AttendanceEvents", dto, cancellationToken);

    public Task UpdateAsync(int id, AttendanceEventDto dto, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/AttendanceEvents/{id}", dto, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/api/AttendanceEvents/{id}", cancellationToken);

    private async Task<List<AttendanceEventDto>> GetEventListAsync(string uri, CancellationToken cancellationToken)
    {
        var raw = await GetAsync<JsonElement>(uri, cancellationToken);

        if (raw.ValueKind == JsonValueKind.Array)
        {
            return raw.Deserialize<List<AttendanceEventDto>>(AttendanceApiJson.SerializerOptions) ?? [];
        }

        if (raw.ValueKind == JsonValueKind.Object)
        {
            if (raw.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                return itemsElement.Deserialize<List<AttendanceEventDto>>(AttendanceApiJson.SerializerOptions) ?? [];
            }

            if (raw.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.Array)
            {
                return valueElement.Deserialize<List<AttendanceEventDto>>(AttendanceApiJson.SerializerOptions) ?? [];
            }
        }

        return [];
    }
}
