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
            builder.Services.AddHttpClient<IUpdateService, UpdateService>();

            builder.Services.AddSingleton(_ =>
                new JournalDbContext(Path.Combine(FileSystem.AppDataDirectory, "journal.db3")));
            builder.Services.AddSingleton<ISessionState, SessionState>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<ISyncNotificationService, SyncNotificationService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IJournalRepository, JournalRepository>();
            builder.Services.AddSingleton<ISettingsService, AppSettingsService>();
            builder.Services.AddSingleton<ILoadingService, LoadingService>();

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
                ClientId = google.GetProperty("ClientId").GetString() ?? string.Empty,
                ClientSecret = google.GetProperty("ClientSecret").GetString() ?? string.Empty
            };
        }
    }
}
