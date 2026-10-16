namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendeeDto
{
    public int? AttendeeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? SchoolName { get; set; }
    public AttendeeType AttendeeType { get; set; }
    public bool Active { get; set; }
}
