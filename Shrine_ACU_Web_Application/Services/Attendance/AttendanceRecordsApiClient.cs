using Shrine_ACU_Web_Application.Models.Attendance;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class AttendanceRecordsApiClient(IHttpClientFactory httpClientFactory, ILogger<AttendanceRecordsApiClient> logger)
    : AttendanceApiClientBase(httpClientFactory, logger)
{
    public async Task<List<AttendanceRecordDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<AttendanceRecordDto>>("/api/AttendanceRecords", cancellationToken) ?? [];

    public Task<PagedResult<AttendanceRecordDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync<AttendanceRecordDto>($"/api/AttendanceRecords/paged?page={page}&pageSize={pageSize}", page, pageSize, cancellationToken);

    public async Task<PagedResult<AttendanceRecordDto>> SearchAsync(AttendanceSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            request.NameOrSchool,
            request.AttendeeType,
            request.EventId,
            AttendanceDate = request.AttendanceDate?.ToString("yyyy-MM-dd"),
            request.Page,
            request.PageSize
        };

        var result = await PostAsync<PagedResult<AttendanceRecordDto>>("/api/AttendanceRecords/search", payload, cancellationToken);
        if (result is not null)
        {
            result.Items ??= [];
            if (result.Page <= 0) result.Page = request.Page;
            if (result.PageSize <= 0) result.PageSize = request.PageSize;
            return result;
        }

        return new PagedResult<AttendanceRecordDto>
        {
            Items = [],
            TotalCount = 0,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public Task<AttendanceSummaryDto?> GetSummaryAsync(int? eventId, DateTime? attendanceDate, CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder()
            .Add("eventId", eventId)
            .Add("attendanceDate", attendanceDate?.ToString("yyyy-MM-dd"))
            .Build();

        return GetAsync<AttendanceSummaryDto>($"/api/AttendanceRecords/summary{query}", cancellationToken);
    }

    public Task<AttendanceRecordDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<AttendanceRecordDto>($"/api/AttendanceRecords/{id}", cancellationToken);

    public Task<AttendanceRecordDto?> SignInAsync(AttendanceSignInRequestDto request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            request.FirstName,
            request.LastName,
            request.SchoolName,
            request.AttendeeType,
            request.EventId,
            AttendanceDate = request.AttendanceDate?.ToString("yyyy-MM-dd")
        };

        return PostAsync<AttendanceRecordDto>("/api/AttendanceRecords/sign-in", payload, cancellationToken);
    }

    public Task<AttendanceRecordDto?> CreateAsync(AttendanceRecordDto dto, CancellationToken cancellationToken = default) =>
        PostAsync<AttendanceRecordDto>("/api/AttendanceRecords", dto, cancellationToken);

    public Task UpdateAsync(int id, AttendanceRecordDto dto, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/AttendanceRecords/{id}", dto, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/api/AttendanceRecords/{id}", cancellationToken);

    public Task<(byte[] Data, string? ContentType, string? FileName)> ExportExcelAsync(int? eventId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder()
            .Add("eventId", eventId)
            .Add("fromDate", fromDate?.ToString("yyyy-MM-dd"))
            .Add("toDate", toDate?.ToString("yyyy-MM-dd"))
            .Build();

        return DownloadAsync($"/api/AttendanceRecords/export/excel{query}", cancellationToken);
    }

    public Task<(byte[] Data, string? ContentType, string? FileName)> ExportGoogleSheetsAsync(int? eventId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder()
            .Add("eventId", eventId)
            .Add("fromDate", fromDate?.ToString("yyyy-MM-dd"))
            .Add("toDate", toDate?.ToString("yyyy-MM-dd"))
            .Build();

        return DownloadAsync($"/api/AttendanceRecords/export/google-sheets{query}", cancellationToken);
    }
}
