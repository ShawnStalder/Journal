using Microsoft.Win32;

namespace Journal.App.Services;

/// <summary>Registers the app under the current user's Run key so it starts at sign-in.</summary>
public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Journal";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool isEnabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (isEnabled)
        {
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
