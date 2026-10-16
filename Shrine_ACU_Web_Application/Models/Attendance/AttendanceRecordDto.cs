namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendanceRecordDto
{
    public int? AttendanceRecordId { get; set; }
    public int EventId { get; set; }
    public int AttendeeId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public decimal DurationHours { get; set; }
    public AttendanceEventDto? Event { get; set; }
    public AttendeeDto? Attendee { get; set; }
}
