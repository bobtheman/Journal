using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Journal.Models;
using Journal.Services.Interfaces;

namespace Journal.Services
{
    public class UpdateService : IUpdateService
    {
        // Public repo - the GitHub Contents API is anonymously readable, no token needed.
        // The APK is committed straight into this repo folder rather than published as a
        // GitHub Release, so we list the folder and read the version out of the filename
        // (e.g. "Journal_1_0_6.apk" -> build 6) instead of reading a release tag.
        private const string ContentsUrl = "https://api.github.com/repos/bobtheman/Journal/contents/Journal/Releases/Latest";
        private const string ChangelogUrl = "https://raw.githubusercontent.com/bobtheman/Journal/main/CHANGELOG.md";
        private static readonly Regex VersionPattern = new(@"_(\d+)\.apk$", RegexOptions.IgnoreCase);

        private readonly HttpClient _httpClient;

        public UpdateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AppUpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, ContentsUrl);
                request.Headers.UserAgent.ParseAdd("Journal-App");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var files = await response.Content.ReadFromJsonAsync<GitHubContentItem[]>();
                if (files is null)
                {
                    return null;
                }

                var latestApk = files
                    .Select(f => new { File = f, Version = TryParseVersion(f.name) })
                    .Where(x => x.Version.HasValue)
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

                if (latestApk is null)
                {
                    return null;
                }

                var currentVersion = int.Parse(AppInfo.Current.BuildString);
                if (latestApk.Version!.Value <= currentVersion)
                {
                    return null;
                }

                return new AppUpdateInfo
                {
                    Version = latestApk.Version.Value,
                    DownloadUrl = latestApk.File.download_url,
                    ReleaseNotesUrl = latestApk.File.html_url,
                    ReleaseNotes = await FetchReleaseNotesAsync()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for update: {ex.Message}");
                return null;
            }
        }

        // Best-effort: the update itself must never be blocked by a missing/unparseable
        // CHANGELOG.md, so any failure here just means the modal shows no notes.
        private async Task<string> FetchReleaseNotesAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, ChangelogUrl);
                request.Headers.UserAgent.ParseAdd("Journal-App");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                var markdown = await response.Content.ReadAsStringAsync();
                return ExtractFirstSection(markdown);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        // Pulls the text under the first "## " heading (the most recent release/unreleased
        // section) so the update dialog shows "what's new," not the entire changelog history.
        private static string ExtractFirstSection(string markdown)
        {
            const string heading = "\n## ";
            var start = markdown.IndexOf(heading, StringComparison.Ordinal);
            if (start < 0)
            {
                return string.Empty;
            }

            var contentStart = markdown.IndexOf('\n', start + heading.Length);
            if (contentStart < 0)
            {
                return string.Empty;
            }

            var end = markdown.IndexOf(heading, contentStart, StringComparison.Ordinal);
            var section = end < 0 ? markdown[contentStart..] : markdown[contentStart..end];
            return section.Trim();
        }

        private static int? TryParseVersion(string fileName)
        {
            var match = VersionPattern.Match(fileName);
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        public async Task DownloadAndInstallAsync(AppUpdateInfo update, IProgress<double>? progress = null)
        {
#if ANDROID
            // Named per version (not a fixed "journal-update.apk") so a file still held open by
            // the Android package installer from a previous install prompt never collides with a
            // fresh download and throws IOException: sharing violation.
            var apkPath = Path.Combine(FileSystem.CacheDirectory, $"journal-update-{update.Version}.apk");
            if (File.Exists(apkPath))
            {
                try
                {
                    File.Delete(apkPath);
                }
                catch (IOException)
                {
                    apkPath = Path.Combine(FileSystem.CacheDirectory, $"journal-update-{update.Version}-{Guid.NewGuid():N}.apk");
                }
            }

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
            intent.SetDataAndType(apkUri, Constants.AndroidPackageMimeType);
            intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.GrantReadUriPermission);
            context.StartActivity(intent);
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("Self-update is only supported on Android.");
#endif
        }

        // ReSharper disable InconsistentNaming
        private class GitHubContentItem
        {
            public string name { get; set; } = string.Empty;
            public string download_url { get; set; } = string.Empty;
            public string html_url { get; set; } = string.Empty;
        }
        // ReSharper restore InconsistentNaming
    }
}
