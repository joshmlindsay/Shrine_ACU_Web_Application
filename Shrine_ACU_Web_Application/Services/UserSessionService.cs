using global::AcuCarShowClient;
using AcuCarShowClient.Models;
using System.Text.Json;
using Microsoft.JSInterop;

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

    public bool IsAdmin => HasAdminAccess(CurrentUser);

    public int? EffectiveUserId => EffectiveUser?.UserId;

    public string EffectiveDisplayName =>
        !string.IsNullOrWhiteSpace(EffectiveUser?.DisplayName)
            ? EffectiveUser!.DisplayName!
            : !string.IsNullOrWhiteSpace(EffectiveUser?.Username)
                ? EffectiveUser!.Username!
                : "-";

    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        if (jsRuntime is null)
        {
            return;
        }

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

            if (IsAuthenticated && IsAdmin)
            {
                await LoadAvailableUsersAsync();
            }

            NotifyStateChanged();
        }
        catch
        {
            // localStorage access failed or invalid JSON, continue without cached session
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
            if (user is null)
            {
                return LoginResult.Fail("User was not found.");
            }

            if (user.IsActive == false)
            {
                return LoginResult.Fail("User is inactive.");
            }

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

            if (IsAdmin)
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
                // localStorage removal failed, but continue with logout
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
        if (!IsAdmin)
        {
            return;
        }

        if (_availableUsers.Count > 0)
        {
            return;
        }

        await LoadAvailableUsersAsync();
        NotifyStateChanged();
    }

    public void SetEffectiveUser(int? userId)
    {
        if (!IsAuthenticated)
        {
            return;
        }

        if (!IsAdmin)
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

    public bool CanEditEntry(global::AcuCarShowClient.Models.CarShowEntryDto? entry)
    {
        if (!IsAuthenticated || entry is null)
        {
            return false;
        }

        if (IsAdmin)
        {
            return true;
        }

        var currentUserId = CurrentUser?.UserId;
        if (!currentUserId.HasValue || !entry.SubmittedByUserId.HasValue)
        {
            return false;
        }

        return entry.SubmittedByUserId == currentUserId.Value;
    }

    private async Task SaveSessionAsync()
    {
        if (_jsRuntime is null)
        {
            return;
        }

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
            // localStorage save failed, but continue without persistence
        }
    }

    private async Task LoadAvailableUsersAsync()
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

    private static bool HasAdminAccess(AppUserDto? user)
    {
        if (user?.ApplicationAccesses is null)
        {
            return false;
        }

        return user.ApplicationAccesses.Any(access =>
            access.CanManage == true ||
            (!string.IsNullOrWhiteSpace(access.AccessLevel) &&
             access.AccessLevel.Contains("admin", StringComparison.OrdinalIgnoreCase)));
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

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public sealed record LoginResult(bool Succeeded, string? ErrorMessage)
    {
        public static LoginResult Success() => new(true, null);

        public static LoginResult Fail(string errorMessage) => new(false, errorMessage);
    }
}
