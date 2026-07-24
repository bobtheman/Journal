using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class AppSettingsService : ISettingsService
    {
        private const string AutoSyncKey = "auto_sync_enabled";
        private const string BackupNotificationsKey = "backup_notifications_enabled";
        private const string LastSyncKey = "last_sync_utc";

        public bool AutoSyncEnabled
        {
            get => Preferences.Default.Get(AutoSyncKey, false);
            set => Preferences.Default.Set(AutoSyncKey, value);
        }

        public bool BackupNotificationsEnabled
        {
            get => Preferences.Default.Get(BackupNotificationsKey, true);
            set => Preferences.Default.Set(BackupNotificationsKey, value);
        }

        public DateTime? LastSyncUtc
        {
            get
            {
                var ticks = Preferences.Default.Get(LastSyncKey, 0L);
                return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
            }
            set => Preferences.Default.Set(LastSyncKey, value?.Ticks ?? 0L);
        }
    }
}
