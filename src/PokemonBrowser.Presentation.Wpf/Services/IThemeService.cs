namespace PokemonBrowser.Presentation.Wpf.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void Initialize();

    void ApplyTheme(AppTheme theme);

    void ApplyThemeToWindow(System.Windows.Window window);
}
