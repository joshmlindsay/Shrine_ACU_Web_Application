using Microsoft.JSInterop;

namespace Shrine_ACU_Web_Application.Services;

public sealed class AppThemeService
{
    private const string SystemValue = "system";
    private const string LightValue = "light";
    private const string DarkValue = "dark";

    private IJSRuntime? _jsRuntime;

    public event Action? StateChanged;

    public string Preference { get; private set; } = SystemValue;

    public static IReadOnlyList<string> ValidPreferences { get; } = [SystemValue, LightValue, DarkValue];

    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        try
        {
            var preference = await jsRuntime.InvokeAsync<string?>("shrineTheme.getPreference");
            Preference = Normalize(preference);
            await jsRuntime.InvokeVoidAsync("shrineTheme.apply", Preference);
        }
        catch
        {
            Preference = SystemValue;
        }

        NotifyStateChanged();
    }

    public async Task SetPreferenceAsync(string? preference)
    {
        var newPreference = Normalize(preference);
        
        if (Preference == newPreference)
        {
            return;
        }

        Preference = newPreference;
        NotifyStateChanged();

        if (_jsRuntime is not null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("shrineTheme.setPreference", Preference);
            }
            catch
            {
            }
        }
    }

    public static string Normalize(string? preference)
    {
        if (string.Equals(preference, LightValue, StringComparison.OrdinalIgnoreCase))
        {
            return LightValue;
        }

        if (string.Equals(preference, DarkValue, StringComparison.OrdinalIgnoreCase))
        {
            return DarkValue;
        }

        return SystemValue;
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
