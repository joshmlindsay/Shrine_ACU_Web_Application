using Shrine_ACU_Web_Application.Models.Attendance;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class AttendeesApiClient(IHttpClientFactory httpClientFactory, ILogger<AttendeesApiClient> logger)
    : AttendanceApiClientBase(httpClientFactory, logger)
{
    public async Task<List<AttendeeDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<AttendeeDto>>("/api/Attendees", cancellationToken) ?? [];

    public Task<PagedResult<AttendeeDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync<AttendeeDto>($"/api/Attendees/paged?page={page}&pageSize={pageSize}", page, pageSize, cancellationToken);

    public Task<PagedResult<AttendeeDto>> SearchAsync(string? nameOrSchool, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder()
            .Add("nameOrSchool", nameOrSchool)
            .Add("page", page)
            .Add("pageSize", pageSize)
            .Build();

        return GetPagedAsync<AttendeeDto>($"/api/Attendees/search{query}", page, pageSize, cancellationToken);
    }

    public Task<AttendeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<AttendeeDto>($"/api/Attendees/{id}", cancellationToken);

    public Task<AttendeeDto?> CreateAsync(AttendeeDto dto, CancellationToken cancellationToken = default) =>
        PostAsync<AttendeeDto>("/api/Attendees", dto, cancellationToken);

    public Task UpdateAsync(int id, AttendeeDto dto, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/Attendees/{id}", dto, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/api/Attendees/{id}", cancellationToken);
}
