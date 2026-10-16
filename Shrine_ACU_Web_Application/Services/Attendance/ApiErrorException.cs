namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class ApiErrorException : Exception
{
    public ApiErrorException(ApiError error)
        : base(error.Message)
    {
        Error = error;
    }

    public ApiError Error { get; }
}
