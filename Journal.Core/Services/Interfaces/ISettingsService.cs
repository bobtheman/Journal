namespace Journal.Services.Interfaces
{
    public interface ISettingsService
    {
        bool AutoSyncEnabled { get; set; }

        DateTime? LastSyncUtc { get; set; }
    }
}
