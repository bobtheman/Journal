namespace Journal.Services.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<bool> IsSignedInAsync();

        Task<bool> SignInAsync();

        Task SignOutAsync();

        Task BackupAsync();

        Task<bool> RestoreLatestAsync();
    }
}
