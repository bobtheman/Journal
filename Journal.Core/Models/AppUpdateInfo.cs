namespace Journal.Models
{
    public class AppUpdateInfo
    {
        public int Version { get; set; } = 0;

        public string DownloadUrl { get; set; } = string.Empty;

        public string ReleaseNotesUrl { get; set; } = string.Empty;

        public string ReleaseNotes { get; set; } = string.Empty;
    }
}
