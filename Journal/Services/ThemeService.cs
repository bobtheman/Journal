namespace Journal.Services
{
    public enum ThemeMode
    {
        System,
        Light,
        Dark
    }

    public class ThemeService
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
