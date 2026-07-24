using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class ThemeService : IThemeService
    {
        private const string ThemeModeKey = "theme_mode";

        public event Action? Changed;

        public ThemeMode Mode
        {
            get => Enum.TryParse<ThemeMode>(Preferences.Default.Get(ThemeModeKey, nameof(ThemeMode.System)), out var mode)
                ? mode
                : ThemeMode.System;
            set
            {
                Preferences.Default.Set(ThemeModeKey, value.ToString());
                Changed?.Invoke();
            }
        }
    }
}
