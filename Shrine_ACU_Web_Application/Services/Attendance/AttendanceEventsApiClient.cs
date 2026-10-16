using Shrine_ACU_Web_Application.Models.Attendance;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class AttendanceEventsApiClient(IHttpClientFactory httpClientFactory, ILogger<AttendanceEventsApiClient> logger)
    : AttendanceApiClientBase(httpClientFactory, logger)
{
    public async Task<List<AttendanceEventDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<AttendanceEventDto>>("/api/AttendanceEvents", cancellationToken) ?? [];

    public Task<PagedResult<AttendanceEventDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync<AttendanceEventDto>($"/api/AttendanceEvents/paged?page={page}&pageSize={pageSize}", page, pageSize, cancellationToken);

    public async Task<List<AttendanceEventDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<AttendanceEventDto>>("/api/AttendanceEvents/active", cancellationToken) ?? [];

    public Task<AttendanceEventDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<AttendanceEventDto>($"/api/AttendanceEvents/{id}", cancellationToken);

    public Task<AttendanceEventDto?> CreateAsync(AttendanceEventDto dto, CancellationToken cancellationToken = default) =>
        PostAsync<AttendanceEventDto>("/api/AttendanceEvents", dto, cancellationToken);

    public Task UpdateAsync(int id, AttendanceEventDto dto, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/AttendanceEvents/{id}", dto, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/api/AttendanceEvents/{id}", cancellationToken);
}
