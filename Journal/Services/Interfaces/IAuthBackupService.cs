using Google.Apis.Drive.v3;

namespace Journal.Services.Interfaces
{
    public interface IAuthBackupService
    {
        Task UploadAsync(DriveService driveService, string folderId);

        Task<bool> ExistsAsync(DriveService driveService, string folderId);

        Task<bool> DownloadAsync(DriveService driveService, string folderId);
    }
}
