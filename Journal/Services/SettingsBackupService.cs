using System.Text.Json;
using Google.Apis.Drive.v3;
using Journal.Models;
using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class SettingsBackupService : ISettingsBackupService
    {
        private readonly ISettingsService _settingsService;
        private readonly BackupOptions _backupOptions;

        public SettingsBackupService(ISettingsService settingsService, BackupOptions backupOptions)
        {
            _settingsService = settingsService;
            _backupOptions = backupOptions;
        }

        public async Task UploadAsync(DriveService driveService, string folderId)
        {
            var settings = new SettingsBackup
            {
                AutoSyncEnabled = _settingsService.AutoSyncEnabled,
                BackupNotificationsEnabled = _settingsService.BackupNotificationsEnabled,
                WifiOnlyBackup = _settingsService.WifiOnlyBackup
            };

            using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(settings));

            var existingId = await FindSettingsFileIdAsync(driveService, folderId);
            if (existingId is null)
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = _backupOptions.SettingsFileName,
                    Parents = [folderId]
                };
                await driveService.Files.Create(fileMetadata, stream, Constants.JsonMimeType).UploadAsync();
            }
            else
            {
                await driveService.Files.Update(new Google.Apis.Drive.v3.Data.File(), existingId, stream, Constants.JsonMimeType).UploadAsync();
            }
        }

        public async Task DownloadAsync(DriveService driveService, string folderId)
        {
            var settingsId = await FindSettingsFileIdAsync(driveService, folderId);
            if (settingsId is null)
            {
                return;
            }

            using var stream = new MemoryStream();
            await driveService.Files.Get(settingsId).DownloadAsync(stream);
            stream.Position = 0;

            var settings = JsonSerializer.Deserialize<SettingsBackup>(stream);
            if (settings is null)
            {
                return;
            }

            _settingsService.AutoSyncEnabled = settings.AutoSyncEnabled;
            _settingsService.BackupNotificationsEnabled = settings.BackupNotificationsEnabled;
            _settingsService.WifiOnlyBackup = settings.WifiOnlyBackup;
        }

        private async Task<string?> FindSettingsFileIdAsync(DriveService driveService, string folderId)
        {
            var listRequest = driveService.Files.List();
            listRequest.Q = $"'{folderId}' in parents and name = '{_backupOptions.SettingsFileName}' and trashed = false";
            listRequest.Fields = Constants.DriveFilesListFields;
            var result = await listRequest.ExecuteAsync();
            return result.Files?.FirstOrDefault()?.Id;
        }

        private class SettingsBackup
        {
            public bool AutoSyncEnabled { get; set; }
            public bool BackupNotificationsEnabled { get; set; }
            public bool WifiOnlyBackup { get; set; }
        }
    }
}
