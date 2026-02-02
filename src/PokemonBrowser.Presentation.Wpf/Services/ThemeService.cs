using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PokemonBrowser.Presentation.Wpf.Services;

public sealed class ThemeService : IThemeService
{
    private readonly ThemeSettingsStore _store;

    public ThemeService(ThemeSettingsStore store)
    {
        _store = store;
    }

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public void Initialize()
    {
        var saved = _store.LoadTheme();
        ApplyTheme(saved ?? CurrentTheme);
    }

    public void ApplyTheme(AppTheme theme)
    {
        if (System.Windows.Application.Current is null)
        {
            return;
        }

        var merged = System.Windows.Application.Current.Resources.MergedDictionaries;

        var existingTheme = merged.FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/Theme.") == true);
        if (existingTheme is not null)
        {
            merged.Remove(existingTheme);
        }

        var uri = theme switch
        {
            AppTheme.Dark => new Uri("Themes/Theme.Dark.xaml", UriKind.Relative),
            _ => new Uri("Themes/Theme.Light.xaml", UriKind.Relative),
        };

        merged.Insert(0, new System.Windows.ResourceDictionary { Source = uri });

        CurrentTheme = theme;
        _store.SaveTheme(theme);

        // Best-effort: update title bar for any windows that already exist.
        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        {
            ApplyThemeToWindow(window);
        }
    }

    public void ApplyThemeToWindow(System.Windows.Window window)
    {
        // Keeps the native window chrome aligned with the app theme. This is best-effort and
        // will no-op on unsupported Windows builds.
        TryApplyImmersiveDarkTitleBar(window, CurrentTheme == AppTheme.Dark);
    }

    private static void TryApplyImmersiveDarkTitleBar(System.Windows.Window window, bool enabled)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                // If the handle isn't created yet, apply once the source initializes.
                void OnSourceInitialized(object? sender, EventArgs args)
                {
                    window.SourceInitialized -= OnSourceInitialized;
                    TryApplyImmersiveDarkTitleBar(window, enabled);
                }

                window.SourceInitialized += OnSourceInitialized;
                return;
            }

            var useDark = enabled ? 1 : 0;

            // 19 = Win10 1809+, 20 = Win11+ (value varies by OS build).
            _ = DwmSetWindowAttribute(handle, 19, ref useDark, Marshal.SizeOf<int>());
            _ = DwmSetWindowAttribute(handle, 20, ref useDark, Marshal.SizeOf<int>());

            // Force a non-client area redraw.
            _ = SetWindowPos(
                handle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
        catch
        {
            // best-effort
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
}
