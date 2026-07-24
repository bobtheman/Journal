using Journal.Services.Interfaces;

namespace Journal.Services
{
    // Lets background sync work (which runs outside any page's lifetime) notify the
    // always-mounted MainLayout so it can show a toast, regardless of which page or
    // dialog kicked the sync off.
    public class SyncNotificationService : ISyncNotificationService
    {
        public event Action? BackupCompleted;

        public void NotifyBackupCompleted() => BackupCompleted?.Invoke();
    }
}
