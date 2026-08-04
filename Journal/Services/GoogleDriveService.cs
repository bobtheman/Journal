using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Journal.Data;
using Journal.Exceptions;
using Journal.Models;
using Journal.Services.Interfaces;
using Microsoft.Maui.ApplicationModel;

namespace Journal.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GoogleAuthOptions _options;
        private readonly HttpClient _httpClient;
        private readonly JournalDbContext _dbContext;
        private readonly ISettingsService _settingsService;
        private readonly ISettingsBackupService _settingsBackupService;
        private readonly IAuthBackupService _authBackupService;

        public GoogleDriveService(
            GoogleAuthOptions options,
            HttpClient httpClient,
            JournalDbContext dbContext,
            ISettingsService settingsService,
            ISettingsBackupService settingsBackupService,
            IAuthBackupService authBackupService)
        {
            _options = options;
            _httpClient = httpClient;
            _dbContext = dbContext;
            _settingsService = settingsService;
            _settingsBackupService = settingsBackupService;
            _authBackupService = authBackupService;
        }

        public async Task<bool> IsSignedInAsync()
        {
            var tokens = await LoadTokensAsync();
            return tokens is not null;
        }

        public async Task<bool> SignInAsync()
        {
            var verifier = PkceHelper.GenerateCodeVerifier();
            var challenge = PkceHelper.GenerateCodeChallenge(verifier);
            var redirectUri = GetRedirectUri();
            var authUrl = BuildAuthUrl(challenge, redirectUri);

            var code = await GetAuthorizationCodeViaLoopbackAsync(authUrl, redirectUri);
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            var tokens = await ExchangeCodeForTokensAsync(code, verifier, redirectUri);
            if (tokens is null)
            {
                return false;
            }

            await SaveTokensAsync(tokens);
            return true;
        }

        private string BuildAuthUrl(string challenge, string redirectUri) =>
            _options.AuthUrl +
            $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(_options.Scope)}" +
            "&access_type=offline&prompt=consent" +
            $"&code_challenge={challenge}&code_challenge_method=S256";

        // Opens the auth page via Custom Tabs (Android) / SFSafariViewController (iOS, Mac
        // Catalyst), which run as part of the app's own task. Once the local listener catches
        // the redirect, we bring MainActivity back to the foreground on Android so the Custom
        // Tab is dismissed automatically. On other platforms the user switches back manually.
        private static async Task<string?> GetAuthorizationCodeViaLoopbackAsync(string authUrl, string redirectUri)
        {
            using var listener = new System.Net.HttpListener();
            listener.Prefixes.Add(redirectUri);
            listener.Start();

            await Browser.Default.OpenAsync(new Uri(authUrl), BrowserLaunchMode.SystemPreferred);

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            const string responseHtml = "<html><body>Signed in. Returning to Journal...</body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();

            listener.Stop();

#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity is not null)
            {
                var intent = new Android.Content.Intent(activity, activity.GetType());
                intent.SetFlags(Android.Content.ActivityFlags.ReorderToFront);
                activity.StartActivity(intent);
            }
#endif

            return code;
        }

        public Task SignOutAsync()
        {
            SecureStorage.Default.Remove(Constants.TokenStorageKey);
            return Task.CompletedTask;
        }

        public async Task BackupAsync()
        {
            var driveService = await GetDriveServiceAsync();
            var folderId = await EnsureBackupFolderAsync(driveService);

            var dbPath = _dbContext.DbPath;

            var fileName = $"{Constants.DatabaseBackupNamePrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss}.db3";
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = [folderId]
            };

            await using var stream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var request = driveService.Files.Create(fileMetadata, stream, Constants.OctetStreamMimeType);
            request.Fields = Constants.DriveFileIdField;
            await request.UploadAsync();

            var uploadedId = request.ResponseBody.Id;

            await _settingsBackupService.UploadAsync(driveService, folderId);
            await _authBackupService.UploadAsync(driveService, folderId);

            await DeleteOldDatabaseBackupsAsync(driveService, folderId, uploadedId);

            _settingsService.LastSyncUtc = DateTime.UtcNow;
        }

        private static async Task DeleteOldDatabaseBackupsAsync(DriveService driveService, string folderId, string keepFileId)
        {
            var listRequest = driveService.Files.List();
            listRequest.Q = $"'{folderId}' in parents and name contains '{Constants.DatabaseBackupNamePrefix}' and trashed = false";
            listRequest.Fields = Constants.DriveFilesListFields;
            var result = await listRequest.ExecuteAsync();

            var oldBackups = result.Files?.Where(file => file.Id != keepFileId) ?? [];
            foreach (var oldBackup in oldBackups)
            {
                await driveService.Files.Delete(oldBackup.Id).ExecuteAsync();
            }
        }

        public async Task<bool> RestoreLatestAsync()
        {
            var driveService = await GetDriveServiceAsync();
            var folderId = await EnsureBackupFolderAsync(driveService);

            var listRequest = driveService.Files.List();
            listRequest.Q = $"'{folderId}' in parents and name contains '{Constants.DatabaseBackupNamePrefix}' and trashed = false";
            listRequest.OrderBy = Constants.DriveModifiedTimeDescending;
            listRequest.PageSize = 1;
            listRequest.Fields = Constants.DriveFilesListFields;

            var result = await listRequest.ExecuteAsync();
            var latest = result.Files?.FirstOrDefault();
            if (latest is null)
            {
                return false;
            }

            if (!await _authBackupService.ExistsAsync(driveService, folderId))
            {
                throw new IncompatibleBackupException(
                    "This backup was made before account credentials were included and can't be restored. Sign in on the original device and create a new backup first.");
            }

            await _dbContext.CloseAsync();
            var dbPath = _dbContext.DbPath;

            await using var output = System.IO.File.Create(dbPath);
            await driveService.Files.Get(latest.Id).DownloadAsync(output);

            await _settingsBackupService.DownloadAsync(driveService, folderId);
            await _authBackupService.DownloadAsync(driveService, folderId);

            _settingsService.LastSyncUtc = DateTime.UtcNow;
            return true;
        }

        private async Task<DriveService> GetDriveServiceAsync()
        {
            var tokens = await GetValidTokensAsync()
                ?? throw new InvalidOperationException("Not signed in to Google Drive.");

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(tokens.AccessToken),
                ApplicationName = "Journal"
            });
        }

        private static async Task<string> EnsureBackupFolderAsync(DriveService driveService)
        {
            var listRequest = driveService.Files.List();
            listRequest.Q = $"name = '{Constants.BackupFolderName}' and mimeType = '{Constants.GoogleDriveFolderMimeType}' and trashed = false";
            listRequest.Fields = Constants.DriveFilesListFields;
            var result = await listRequest.ExecuteAsync();

            var existing = result.Files?.FirstOrDefault();
            if (existing is not null)
            {
                return existing.Id;
            }

            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = Constants.BackupFolderName,
                MimeType = Constants.GoogleDriveFolderMimeType
            };
            var created = await driveService.Files.Create(folderMetadata).ExecuteAsync();
            return created.Id;
        }

        private string GetRedirectUri() => $"http://{_options.LoopbackHost}:{_options.LoopbackPort}/";

        private async Task<TokenData?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri)
        {
            var form = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier
            };

            var response = await _httpClient.PostAsync(
                _options.TokenUrl, new FormUrlEncodedContent(form));

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var payload = JsonSerializer.Deserialize<TokenExchangeResponse>(json)!;

            return new TokenData
            {
                AccessToken = payload.access_token,
                RefreshToken = payload.refresh_token,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(payload.expires_in)
            };
        }

        private async Task<TokenData?> RefreshTokensAsync(TokenData tokens)
        {
            if (string.IsNullOrEmpty(tokens.RefreshToken))
            {
                return null;
            }

            var form = new Dictionary<string, string>
            {
                ["refresh_token"] = tokens.RefreshToken,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "refresh_token"
            };

            var response = await _httpClient.PostAsync(
                _options.TokenUrl, new FormUrlEncodedContent(form));

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var payload = JsonSerializer.Deserialize<TokenExchangeResponse>(json)!;

            var refreshed = new TokenData
            {
                AccessToken = payload.access_token,
                RefreshToken = tokens.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(payload.expires_in)
            };

            await SaveTokensAsync(refreshed);
            return refreshed;
        }

        private async Task<TokenData?> GetValidTokensAsync()
        {
            var tokens = await LoadTokensAsync();
            if (tokens is null)
            {
                return null;
            }

            if (TokenExpiryHelper.IsExpiredOrExpiring(tokens.ExpiresAtUtc))
            {
                return await RefreshTokensAsync(tokens);
            }

            return tokens;
        }

        private static async Task<TokenData?> LoadTokensAsync()
        {
            var json = await SecureStorage.Default.GetAsync(Constants.TokenStorageKey);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<TokenData>(json);
        }

        private static async Task SaveTokensAsync(TokenData tokens)
        {
            await SecureStorage.Default.SetAsync(Constants.TokenStorageKey, JsonSerializer.Serialize(tokens));
        }
    }
}
