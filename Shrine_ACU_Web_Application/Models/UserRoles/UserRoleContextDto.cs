using AcuCarShowClient.Models;

namespace Shrine_ACU_Web_Application.Models.UserRoles;

public sealed class UserRoleContextDto
{
    public int? UserId { get; set; }
    public int? ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public UserAccessRole? AccessRoles { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanManage { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }
    public bool IsJudge { get; set; }
    public bool IsParticipant { get; set; }
}