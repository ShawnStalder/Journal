using Journal.Core.State;

namespace Journal.Tests;

public sealed class StateTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"JournalState_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Settings_DefaultToTheDarkTheme()
    {
        var settings = new SettingsStore(_folder).Load();

        Assert.True(settings.IsDarkTheme);
    }

    [Fact]
    public void Settings_RoundTrip()
    {
        var store = new SettingsStore(_folder);

        store.Save(new AppSettings { IsDarkTheme = false });

        Assert.False(store.Load().IsDarkTheme);
    }

    [Fact]
    public void Settings_FallBackToDefaultsWhenTheFileIsCorrupt()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "settings.json"), "{ nope");

        Assert.True(new SettingsStore(_folder).Load().IsDarkTheme);
    }

    [Fact]
    public void Session_IsOnlyRestoredOnce()
    {
        var store = new SessionStore(_folder);
        store.Save(["One", "Two"]);

        Assert.Equal(["One", "Two"], store.Take());
        Assert.Empty(store.Take());
    }
}
