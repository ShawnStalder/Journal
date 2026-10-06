using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Journal.App.Services;

public static class TitleBarTheme
{
    private const int UseImmersiveDarkModeAttribute = 20;

    public static void Apply(Window window, bool isDark)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var value = isDark ? 1 : 0;
        DwmSetWindowAttribute(handle, UseImmersiveDarkModeAttribute, ref value, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int size);
}
