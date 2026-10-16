namespace Shrine_ACU_Web_Application.Models.UserRoles;

public sealed class UserEventRoleCheckResultDto
{
    public int? UserId { get; set; }
    public int? EventId { get; set; }
    public string? Role { get; set; }
    public bool HasRole { get; set; }
}