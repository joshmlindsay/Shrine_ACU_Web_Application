namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class ApiError
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = "Unexpected API error.";
    public string? RawBody { get; init; }
}
