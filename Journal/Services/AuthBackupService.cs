using System.Text.Json;
using Google.Apis.Drive.v3;
using Journal.Models;
using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class AuthBackupService : IAuthBackupService
    {
        private readonly BackupOptions _backupOptions;

        public AuthBackupService(BackupOptions backupOptions)
        {
            _backupOptions = backupOptions;
        }

        public async Task UploadAsync(DriveService driveService, string folderId)
        {
            var saltHex = Preferences.Default.Get(Constants.AuthSaltKey, string.Empty);
            if (string.IsNullOrEmpty(saltHex))
            {
                return;
            }

            var auth = new AuthBackup
            {
                SaltHex = saltHex,
                Username = Preferences.Default.Get(Constants.AuthUsernameKey, string.Empty)
            };

            using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(auth));

            var existingId = await FindAuthFileIdAsync(driveService, folderId);
            if (existingId is null)
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = _backupOptions.AuthFileName,
                    Parents = [folderId]
                };
                await driveService.Files.Create(fileMetadata, stream, Constants.JsonMimeType).UploadAsync();
            }
            else
            {
                await driveService.Files.Update(new Google.Apis.Drive.v3.Data.File(), existingId, stream, Constants.JsonMimeType).UploadAsync();
            }
        }

        public async Task<bool> ExistsAsync(DriveService driveService, string folderId)
        {
            return await FindAuthFileIdAsync(driveService, folderId) is not null;
        }

        public async Task<bool> DownloadAsync(DriveService driveService, string folderId)
        {
            var authId = await FindAuthFileIdAsync(driveService, folderId);
            if (authId is null)
            {
                return false;
            }

            using var stream = new MemoryStream();
            await driveService.Files.Get(authId).DownloadAsync(stream);
            stream.Position = 0;

            var auth = JsonSerializer.Deserialize<AuthBackup>(stream);
            if (auth is null || string.IsNullOrEmpty(auth.SaltHex))
            {
                return false;
            }

            Preferences.Default.Set(Constants.AuthSaltKey, auth.SaltHex);
            Preferences.Default.Set(Constants.AuthUsernameKey, auth.Username);
            return true;
        }

        private async Task<string?> FindAuthFileIdAsync(DriveService driveService, string folderId)
        {
            var listRequest = driveService.Files.List();
            listRequest.Q = $"'{folderId}' in parents and name = '{_backupOptions.AuthFileName}' and trashed = false";
            listRequest.Fields = Constants.DriveFilesListFields;
            var result = await listRequest.ExecuteAsync();
            return result.Files?.FirstOrDefault()?.Id;
        }
    }
}
