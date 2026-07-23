using Journal.Data;
using Journal.Models;
using Journal.Services;
using Journal.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Journal
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();

            builder.Services.AddSingleton(LoadGoogleAuthOptions());
            builder.Services.AddHttpClient<IGoogleDriveService, GoogleDriveService>();

            builder.Services.AddSingleton(_ =>
                new JournalDbContext(Path.Combine(FileSystem.AppDataDirectory, "journal.db3")));
            builder.Services.AddSingleton<SessionState>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IJournalRepository, JournalRepository>();
            builder.Services.AddSingleton<ISettingsService, AppSettingsService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static GoogleAuthOptions LoadGoogleAuthOptions()
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
            using var document = System.Text.Json.JsonDocument.Parse(stream);
            var google = document.RootElement.GetProperty("Google");

            return new GoogleAuthOptions
            {
                AndroidClientId = google.GetProperty("AndroidClientId").GetString() ?? string.Empty
            };
        }
    }
}
