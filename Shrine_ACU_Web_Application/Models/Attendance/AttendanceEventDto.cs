namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendanceEventDto
{
    public int? EventId { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public DateTime? EventDate { get; set; }
    public AttendanceEventDateDto[] EventDates { get; set; } = [];
    public bool VolunteerHoursApplicable { get; set; }
    public decimal? DefaultVolunteerHours { get; set; }
    public bool Active { get; set; }
}
