using AcuCarShowClient.Models;

namespace Shrine_ACU_Web_Application.Models.UserRoles;

public sealed class UserEventRoleContextDto
{
    public int? UserId { get; set; }
    public int? EventId { get; set; }
    public string? EventName { get; set; }
    public UserAccessRole? AccessRoles { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanManage { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }
    public bool IsJudge { get; set; }
    public bool IsParticipant { get; set; }
}