using Journal.Services;

namespace Journal.Services.Interfaces
{
    public interface IThemeService
    {
        event Action? Changed;

        ThemeMode Mode { get; set; }
    }
}
