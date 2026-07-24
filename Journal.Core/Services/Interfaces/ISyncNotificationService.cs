namespace Journal.Services.Interfaces
{
    public interface ISyncNotificationService
    {
        event Action? BackupCompleted;

        void NotifyBackupCompleted();
    }
}
