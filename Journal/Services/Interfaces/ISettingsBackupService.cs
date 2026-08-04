using Google.Apis.Drive.v3;

namespace Journal.Services.Interfaces
{
    public interface ISettingsBackupService
    {
        Task UploadAsync(DriveService driveService, string folderId);

        Task DownloadAsync(DriveService driveService, string folderId);
    }
}
