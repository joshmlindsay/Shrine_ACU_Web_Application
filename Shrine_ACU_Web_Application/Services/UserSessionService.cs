using global::AcuCarShowClient;
using AcuCarShowClient.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Shrine_ACU_Web_Application.Services;

public sealed class UserSessionService
{
    private const string CurrentUserStorageKey = "shrine_current_user";
    private const string EffectiveUserStorageKey = "shrine_effective_user";

    private readonly global::AcuCarShowClient.AcuCarShowClient _client;
    private readonly List<AppUserDto> _availableUsers = [];
    private IJSRuntime? _jsRuntime;

    public UserSessionService(global::AcuCarShowClient.AcuCarShowClient client)
    {
        _client = client;
    }

    public event Action? StateChanged;

    public AppUserDto? CurrentUser { get; private set; }
    public AppUserDto? EffectiveUser { get; private set; }

    public IReadOnlyList<AppUserDto> AvailableUsers => _availableUsers;
    public bool IsAuthenticated => CurrentUser is not null;
    public IReadOnlyList<UserApplicationAccessDto> Accesses => CurrentUser?.ApplicationAccesses ?? [];

    private PermissionPolicy CurrentPolicy => ResolvePolicy(Accesses);

    public bool IsAdmin => CurrentPolicy.IsAdmin;
    public bool CanManage => CurrentPolicy.CanManage;
    public bool CanWrite => CurrentPolicy.CanWrite;
    public bool CanRead => CurrentPolicy.CanRead;
    public bool CanManageUsers => CurrentPolicy.CanManageUsers;
    public bool CanAccessScoring => CurrentPolicy.CanAccessScoring;

    public int? EffectiveUserId => EffectiveUser?.UserId;

    public string EffectiveDisplayName =>
        !string.IsNullOrWhiteSpace(EffectiveUser?.DisplayName)
            ? EffectiveUser!.DisplayName!
            : !string.IsNullOrWhiteSpace(EffectiveUser?.Username)
                ? EffectiveUser!.Username!
                : "-";

    public IReadOnlyList<string> RoleLabels => BuildRoleLabels(Accesses);

    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        if (jsRuntime is null) return;

        try
        {
            var userJson = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", CurrentUserStorageKey);
            if (!string.IsNullOrWhiteSpace(userJson))
            {
                CurrentUser = JsonSerializer.Deserialize<AppUserDto>(userJson);
            }

            var effectiveUserJson = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", EffectiveUserStorageKey);
            if (!string.IsNullOrWhiteSpace(effectiveUserJson))
            {
                EffectiveUser = JsonSerializer.Deserialize<AppUserDto>(effectiveUserJson);
            }

            if (IsAuthenticated && CanManageUsers)
            {
                await LoadAvailableUsersAsync();
            }

            NotifyStateChanged();
        }
        catch
        {
        }
    }

    public async Task<LoginResult> LoginAsync(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return LoginResult.Fail("Username and password are required.");
        }

        try
        {
            var user = await _client.Api.AppUsers.Username[username.Trim()].GetAsync();
            if (user is null) return LoginResult.Fail("User was not found.");
            if (user.IsActive == false) return LoginResult.Fail("User is inactive.");

            if (!string.IsNullOrWhiteSpace(user.Password) && !string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                return LoginResult.Fail("Invalid username or password.");
            }

            if (user.UserId.HasValue && (user.ApplicationAccesses is null || user.ApplicationAccesses.Count == 0))
            {
                user.ApplicationAccesses = await _client.Api.UserApplicationAccess.User[user.UserId.Value].GetAsync() ?? [];
            }

            CurrentUser = user;
            EffectiveUser = user;

            if (CanManageUsers)
            {
                await LoadAvailableUsersAsync();
            }
            else
            {
                _availableUsers.Clear();
                _availableUsers.Add(user);
            }

            await SaveSessionAsync();
            NotifyStateChanged();
            return LoginResult.Success();
        }
        catch (Exception ex)
        {
            return LoginResult.Fail($"Unable to login. {ex.Message}");
        }
    }

    public async Task<OperationResult> RefreshCurrentUserAsync()
    {
        if (CurrentUser?.UserId is not int currentUserId)
        {
            return OperationResult.Fail("No active user session.");
        }

        try
        {
            var refreshed = await _client.Api.AppUsers[currentUserId].GetAsync();
            if (refreshed is null)
            {
                return OperationResult.Fail("Unable to refresh user profile.");
            }

            if (refreshed.UserId.HasValue && (refreshed.ApplicationAccesses is null || refreshed.ApplicationAccesses.Count == 0))
            {
                refreshed.ApplicationAccesses = await _client.Api.UserApplicationAccess.User[refreshed.UserId.Value].GetAsync() ?? [];
            }

            CurrentUser = refreshed;
            EffectiveUser ??= refreshed;

            if (CanManageUsers)
            {
                await LoadAvailableUsersAsync();
            }

            await SaveSessionAsync();
            NotifyStateChanged();
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Unable to refresh profile. {ex.Message}");
        }
    }

    public async Task<OperationResult> UpdateCurrentUserProfileAsync(string? displayName, string? firstName, string? lastName, string? email)
    {
        if (CurrentUser?.UserId is not int currentUserId)
        {
            return OperationResult.Fail("No active user session.");
        }

        try
        {
            var user = await _client.Api.AppUsers[currentUserId].GetAsync();
            if (user is null)
            {
                return OperationResult.Fail("Unable to load your profile.");
            }

            user.DisplayName = NullIfWhiteSpace(displayName);
            user.FirstName = NullIfWhiteSpace(firstName);
            user.LastName = NullIfWhiteSpace(lastName);
            user.Email = NullIfWhiteSpace(email);

            await _client.Api.AppUsers[user.UserId!.Value].PutAsync(user);
            return await RefreshCurrentUserAsync();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Unable to update profile. {ex.Message}");
        }
    }

    public async Task<OperationResult> ChangePasswordAsync(string? currentPassword, string? newPassword)
    {
        if (CurrentUser?.UserId is not int currentUserId)
        {
            return OperationResult.Fail("No active user session.");
        }

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return OperationResult.Fail("Current and new password are required.");
        }

        try
        {
            await _client.Api.AppUsers[currentUserId].ChangePassword.PostAsync(new PasswordChangeRequestDto
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

            return OperationResult.Success("Password updated successfully.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Unable to change password. {ex.Message}");
        }
    }

    public async Task<OperationResult> RequestPasswordResetAsync(string? username, string? email)
    {
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(email))
        {
            return OperationResult.Fail("Provide username or email.");
        }

        try
        {
            await _client.Api.AppUsers.RequestPasswordReset.PostAsync(new PasswordResetRequestDto
            {
                Username = NullIfWhiteSpace(username),
                Email = NullIfWhiteSpace(email)
            });

            return OperationResult.Success("If an account matches the request, a reset token has been sent.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Unable to request password reset. {ex.Message}");
        }
    }

    public async Task<OperationResult> CompletePasswordResetAsync(string? username, string? resetToken, string? newPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(resetToken) || string.IsNullOrWhiteSpace(newPassword))
        {
            return OperationResult.Fail("Username, reset token, and new password are required.");
        }

        try
        {
            await _client.Api.AppUsers.ResetPassword.PostAsync(new PasswordResetCompleteDto
            {
                Username = username.Trim(),
                ResetToken = resetToken.Trim(),
                NewPassword = newPassword
            });

            return OperationResult.Success("Password reset completed successfully.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Unable to reset password. {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        EffectiveUser = null;
        _availableUsers.Clear();

        if (_jsRuntime is not null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CurrentUserStorageKey);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EffectiveUserStorageKey);
            }
            catch
            {
            }
        }

        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        EffectiveUser = null;
        _availableUsers.Clear();
        NotifyStateChanged();
    }

    public async Task EnsureAdminUsersLoadedAsync()
    {
        if (!CanManageUsers) return;
        if (_availableUsers.Count > 0) return;

        await LoadAvailableUsersAsync();
        NotifyStateChanged();
    }

    public void SetEffectiveUser(int? userId)
    {
        if (!IsAuthenticated) return;

        if (!CanManageUsers)
        {
            EffectiveUser = CurrentUser;
            NotifyStateChanged();
            return;
        }

        if (!userId.HasValue)
        {
            EffectiveUser = CurrentUser;
            NotifyStateChanged();
            return;
        }

        EffectiveUser = _availableUsers.FirstOrDefault(x => x.UserId == userId) ?? CurrentUser;
        NotifyStateChanged();
    }

    public bool CanEditEntry(CarShowEntryDto? entry)
    {
        if (!IsAuthenticated || entry is null) return false;
        if (CanManage) return true;
        if (!CanWrite) return false;

        var currentUserId = CurrentUser?.UserId;
        if (!currentUserId.HasValue || !entry.SubmittedByUserId.HasValue) return false;

        return entry.SubmittedByUserId == currentUserId.Value;
    }

    private async Task SaveSessionAsync()
    {
        if (_jsRuntime is null) return;

        try
        {
            if (CurrentUser is not null)
            {
                var userJson = JsonSerializer.Serialize(CurrentUser);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CurrentUserStorageKey, userJson);
            }

            if (EffectiveUser is not null)
            {
                var effectiveUserJson = JsonSerializer.Serialize(EffectiveUser);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", EffectiveUserStorageKey, effectiveUserJson);
            }
        }
        catch
        {
        }
    }

    private async Task LoadAvailableUsersAsync()
    {
        try
        {
            var users = (await _client.Api.AppUsers.GetAsync()) ?? [];
            _availableUsers.Clear();

            foreach (var user in users.Where(x => x.IsActive != false).OrderBy(GetUserLabel))
            {
                _availableUsers.Add(user);
            }

            if (CurrentUser is not null && !_availableUsers.Any(x => x.UserId == CurrentUser.UserId))
            {
                _availableUsers.Add(CurrentUser);
            }
        }
        catch
        {
            _availableUsers.Clear();

            if (CurrentUser is not null)
            {
                _availableUsers.Add(CurrentUser);
            }
        }
    }

    private static PermissionPolicy ResolvePolicy(IEnumerable<UserApplicationAccessDto> accesses)
    {
        var policy = PermissionPolicy.None;

        foreach (var access in accesses)
        {
            policy = policy.Merge(ResolvePolicyForAccess(access));
        }

        return policy;
    }

    private static PermissionPolicy ResolvePolicyForAccess(UserApplicationAccessDto access)
    {
        var rolePolicy = PermissionPolicy.None;

        if (access.AccessRoles.HasValue)
        {
            var roles = access.AccessRoles.Value;

            if (roles.HasFlag(UserAccessRole.Admin))
                rolePolicy = rolePolicy.Merge(new PermissionPolicy(CanRead: true, CanWrite: true, CanManage: true, CanManageUsers: true, CanAccessScoring: true, IsAdmin: true));

            if (roles.HasFlag(UserAccessRole.Manager))
                rolePolicy = rolePolicy.Merge(new PermissionPolicy(CanRead: true, CanWrite: true, CanManage: true, CanManageUsers: true, CanAccessScoring: true, IsAdmin: false));

            if (roles.HasFlag(UserAccessRole.Judge))
                rolePolicy = rolePolicy.Merge(new PermissionPolicy(CanRead: true, CanWrite: true, CanManage: false, CanManageUsers: false, CanAccessScoring: true, IsAdmin: false));

            if (roles.HasFlag(UserAccessRole.Participant))
                rolePolicy = rolePolicy.Merge(new PermissionPolicy(CanRead: true, CanWrite: true, CanManage: false, CanManageUsers: false, CanAccessScoring: false, IsAdmin: false));
        }

        var accessLevelPolicy = new PermissionPolicy(
            CanRead: AccessLevelContains(access.AccessLevel, "read") || AccessLevelContains(access.AccessLevel, "view"),
            CanWrite: AccessLevelContains(access.AccessLevel, "write") || AccessLevelContains(access.AccessLevel, "edit"),
            CanManage: AccessLevelContains(access.AccessLevel, "manage") || AccessLevelContains(access.AccessLevel, "admin") || AccessLevelContains(access.AccessLevel, "superadmin"),
            CanManageUsers: AccessLevelContains(access.AccessLevel, "manage") || AccessLevelContains(access.AccessLevel, "admin") || AccessLevelContains(access.AccessLevel, "superadmin"),
            CanAccessScoring: AccessLevelContains(access.AccessLevel, "judge") || AccessLevelContains(access.AccessLevel, "scor") || AccessLevelContains(access.AccessLevel, "admin") || AccessLevelContains(access.AccessLevel, "manage"),
            IsAdmin: AccessLevelContains(access.AccessLevel, "admin") || AccessLevelContains(access.AccessLevel, "superadmin"));

        var flagPolicy = new PermissionPolicy(
            CanRead: access.CanRead == true,
            CanWrite: access.CanWrite == true,
            CanManage: access.CanManage == true,
            CanManageUsers: access.CanManage == true,
            CanAccessScoring: access.CanManage == true || (access.AccessRoles.HasValue && (
                access.AccessRoles.Value.HasFlag(UserAccessRole.Admin) ||
                access.AccessRoles.Value.HasFlag(UserAccessRole.Manager) ||
                access.AccessRoles.Value.HasFlag(UserAccessRole.Judge))),
            IsAdmin: access.CanManage == true && access.AccessRoles.HasValue && access.AccessRoles.Value.HasFlag(UserAccessRole.Admin));

        return rolePolicy.Merge(accessLevelPolicy).Merge(flagPolicy).Normalize();
    }

    private static bool AccessLevelContains(string? accessLevel, string token)
    {
        if (string.IsNullOrWhiteSpace(accessLevel))
        {
            return false;
        }

        return accessLevel.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> BuildRoleLabels(IEnumerable<UserApplicationAccessDto> accesses)
    {
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var access in accesses)
        {
            if (!string.IsNullOrWhiteSpace(access.AccessLevel))
            {
                foreach (var token in access.AccessLevel
                             .Split(new[] { ',', ';', '|', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    labels.Add(token);
                }
            }

            if (access.CanManage == true) labels.Add("Manage");
            if (access.CanWrite == true) labels.Add("Write");
            if (access.CanRead == true) labels.Add("Read");

            if (access.AccessRoles.HasValue && access.AccessRoles != UserAccessRole.None)
            {
                labels.Add($"Role: {access.AccessRoles.Value}");
            }
        }

        return labels.OrderBy(x => x).ToList();
    }

    private static string GetUserLabel(AppUserDto user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.Username ?? string.Empty;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public sealed record LoginResult(bool Succeeded, string? ErrorMessage)
    {
        public static LoginResult Success() => new(true, null);
        public static LoginResult Fail(string errorMessage) => new(false, errorMessage);
    }

    public sealed record OperationResult(bool Succeeded, string? ErrorMessage, string? Message)
    {
        public static OperationResult Success(string? message = null) => new(true, null, message);
        public static OperationResult Fail(string errorMessage) => new(false, errorMessage, null);
    }

    private sealed record PermissionPolicy(bool CanRead, bool CanWrite, bool CanManage, bool CanManageUsers, bool CanAccessScoring, bool IsAdmin)
    {
        public static PermissionPolicy None => new(false, false, false, false, false, false);

        public PermissionPolicy Merge(PermissionPolicy other)
        {
            return new PermissionPolicy(
                CanRead || other.CanRead,
                CanWrite || other.CanWrite,
                CanManage || other.CanManage,
                CanManageUsers || other.CanManageUsers,
                CanAccessScoring || other.CanAccessScoring,
                IsAdmin || other.IsAdmin);
        }

        public PermissionPolicy Normalize()
        {
            var manage = CanManage || IsAdmin;
            var write = CanWrite || manage;
            var read = CanRead || write;
            var manageUsers = CanManageUsers || manage;
            var scoring = CanAccessScoring || manage;
            return new PermissionPolicy(read, write, manage, manageUsers, scoring, IsAdmin);
        }
    }
}
