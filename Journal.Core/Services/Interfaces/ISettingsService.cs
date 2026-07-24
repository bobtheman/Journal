namespace Journal.Services.Interfaces
{
    public interface ISettingsService
    {
        bool AutoSyncEnabled { get; set; }

        bool BackupNotificationsEnabled { get; set; }

        DateTime? LastSyncUtc { get; set; }
    }
}
