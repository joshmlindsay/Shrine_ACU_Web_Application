namespace Shrine_ACU_Web_Application.Models.Attendance;

public sealed class AttendanceSummaryDto
{
    public int TotalRecords { get; set; }
    public decimal TotalDurationHours { get; set; }
    public Dictionary<string, int> CountByType { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
