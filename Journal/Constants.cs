namespace Journal
{
    public static class Constants
    {
        public const string BackupFolderName = "JournalApp";
        public const string DatabaseBackupNamePrefix = "journal-";
        public const string TokenStorageKey = "google_tokens";

        public const string AuthSaltKey = "auth_salt";
        public const string AuthUsernameKey = "auth_username";

        public const string GoogleDriveFolderMimeType = "application/vnd.google-apps.folder";
        public const string OctetStreamMimeType = "application/octet-stream";
        public const string JsonMimeType = "application/json";
        public const string AndroidPackageMimeType = "application/vnd.android.package-archive";

        public const string DriveFilesListFields = "files(id, name)";
        public const string DriveFileIdField = "id";
        public const string DriveModifiedTimeDescending = "modifiedTime desc";
    }
}
