using Journal.Core.State;

namespace Journal.App.Services;

public sealed class ThemeService
{
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;

    public ThemeService(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _settings = settingsStore.Load();
    }

    public event Action? Changed;

    public bool IsDark => _settings.IsDarkTheme;

    public void SetDark(bool isDark)
    {
        if (_settings.IsDarkTheme == isDark)
        {
            return;
        }

        _settings.IsDarkTheme = isDark;
        _settingsStore.Save(_settings);
        Changed?.Invoke();
    }
}
