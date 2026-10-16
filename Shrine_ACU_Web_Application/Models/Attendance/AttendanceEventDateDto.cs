namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendanceEventDateDto
{
    public int? AttendanceEventDateId { get; set; }
    public int? EventId { get; set; }
    public DateOnly EventDate { get; set; }
}
