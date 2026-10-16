using AcuCarShowClient.Models;

namespace Shrine_ACU_Web_Application.Models.UserRoles;

public sealed class UserRoleUpsertRequestDto
{
    public UserAccessRole AccessRoles { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanManage { get; set; }
}