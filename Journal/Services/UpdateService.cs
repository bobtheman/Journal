using System.Net.Http.Json;
using Journal.Models;
using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class UpdateService : IUpdateService
    {
        // Public repo - the GitHub API is anonymously readable, no token needed.
        private const string LatestReleaseUrl = "https://github.com/bobtheman/Journal/tree/main/Journal/Releases/Latest";

        private readonly HttpClient _httpClient;

        public UpdateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AppUpdateInfo?> CheckForUpdateAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
            request.Headers.UserAgent.ParseAdd("Journal-App");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();
            if (release is null || !TryParseVersion(release.tag_name, out var latestVersion))
            {
                return null;
            }

            var currentVersion = int.Parse(AppInfo.Current.BuildString);
            if (latestVersion <= currentVersion)
            {
                return null;
            }

            var apkAsset = release.assets.FirstOrDefault(a => a.name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));
            if (apkAsset is null)
            {
                return null;
            }

            return new AppUpdateInfo
            {
                Version = latestVersion,
                DownloadUrl = apkAsset.browser_download_url,
                ReleaseNotesUrl = release.html_url
            };
        }

        private static bool TryParseVersion(string tagName, out int version) =>
            int.TryParse(tagName.TrimStart('v', 'V'), out version);

        public async Task DownloadAndInstallAsync(AppUpdateInfo update, IProgress<double>? progress = null)
        {
#if ANDROID
            var apkPath = Path.Combine(FileSystem.CacheDirectory, "journal-update.apk");

            using (var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(apkPath);

                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                while ((read = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    totalRead += read;
                    if (totalBytes > 0)
                    {
                        progress?.Report((double)totalRead / totalBytes);
                    }
                }
            }

            var context = Android.App.Application.Context;
            var apkFile = new Java.IO.File(apkPath);
            var apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context, $"{context.PackageName}.fileProvider", apkFile);

            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.GrantReadUriPermission);
            context.StartActivity(intent);
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("Self-update is only supported on Android.");
#endif
        }

        // ReSharper disable InconsistentNaming
        private class GitHubRelease
        {
            public string tag_name { get; set; } = string.Empty;
            public string html_url { get; set; } = string.Empty;
            public GitHubAsset[] assets { get; set; } = [];
        }

        private class GitHubAsset
        {
            public string name { get; set; } = string.Empty;
            public string browser_download_url { get; set; } = string.Empty;
        }
        // ReSharper restore InconsistentNaming
    }
}
