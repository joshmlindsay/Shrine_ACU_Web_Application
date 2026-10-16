namespace Shrine_ACU_Web_Application.Models.UserRoles;

public sealed class UserRoleCheckResultDto
{
    public int? UserId { get; set; }
    public int? ApplicationId { get; set; }
    public string? Role { get; set; }
    public bool HasRole { get; set; }
}