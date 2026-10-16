namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendanceSearchRequestDto
{
    public string? NameOrSchool { get; set; }
    public AttendeeType? AttendeeType { get; set; }
    public int? EventId { get; set; }
    public DateTime? AttendanceDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
