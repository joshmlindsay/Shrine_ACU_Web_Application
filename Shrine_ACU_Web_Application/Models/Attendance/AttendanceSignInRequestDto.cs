namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendanceSignInRequestDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? SchoolName { get; set; }
    public AttendeeType AttendeeType { get; set; }
    public int EventId { get; set; }
    public DateTime? AttendanceDate { get; set; }
}
