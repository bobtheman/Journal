using Journal.Models;
using Journal.Services.Interfaces;

namespace Journal.Services
{
    // Self-update (GitHub-hosted APK check + in-app install) was removed for the Google
    // Play release: it required android.permission.REQUEST_INSTALL_PACKAGES, which Play
    // rejects outside a short list of permitted app categories (browsers, file managers,
    // etc.) that this app doesn't belong to. Play owns updates for Play-distributed installs
    // anyway. Kept as a no-op implementation so callers (Settings, MainLayout) don't need to
    // special-case update checks away.
    public class UpdateService : IUpdateService
    {
        public Task<AppUpdateInfo?> CheckForUpdateAsync()
        {
            return Task.FromResult<AppUpdateInfo?>(null);
        }

        public Task DownloadAndInstallAsync(AppUpdateInfo update, IProgress<double>? progress = null)
        {
            throw new PlatformNotSupportedException("Self-update was removed for Google Play compliance.");
        }
    }
}
