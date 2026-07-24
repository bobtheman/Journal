using Journal.Models;

namespace Journal.Services.Interfaces
{
    public interface IUpdateService
    {
        // Returns update info if a newer version is available on GitHub Releases, else null.
        Task<AppUpdateInfo?> CheckForUpdateAsync();

        // Downloads the release APK and hands it to the OS installer. Android only -
        // other platforms don't support sideloaded self-update.
        Task DownloadAndInstallAsync(AppUpdateInfo update, IProgress<double>? progress = null);
    }
}
