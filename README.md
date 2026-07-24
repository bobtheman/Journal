# Journal

A personal journal app built with .NET MAUI Blazor Hybrid, targeting Android, iOS, Mac Catalyst, and Windows.

## Projects

- **Journal** — MAUI Blazor Hybrid app (UI, pages, services, platform heads).
- **Journal.Core** — shared library: EF Core data model (`JournalDbContext`), domain models, repository, auth/crypto helpers (PBKDF2 key derivation, PKCE, token expiry).
- **Journal.Tests** — test project.

## Features

- Journal entries with mood tagging (`Components/Pages/JournalHome.razor`, `JournalEntryDialog.razor`).
- Google Drive backup/sync via `GoogleDriveService` and Google API OAuth (PKCE).
- Biometric login (`Plugin.Maui.Biometric`).
- Light/dark theming (`ThemeService`).

## Tech Stack

- .NET 10, MAUI, Blazor Hybrid, MudBlazor
- EF Core (`Journal.Core/Data/JournalDbContext.cs`)
- Google.Apis.Auth / Google.Apis.Drive.v3

## Getting Started

```
dotnet build Journal.slnx
```

Run a specific target framework (e.g. Windows):

```
dotnet build Journal/Journal.csproj -f net10.0-windows10.0.19041.0
```

App settings live in `Journal/Resources/Raw/appsettings.json` (Google OAuth client config, etc. — not committed if it contains secrets).

## Icons

App icon source: `Journal/Resources/AppIcon/appicon.png` + `appiconfg.png`, built per-platform via the `MauiIcon` MSBuild target. Store-listing assets (Google Play / App Store console uploads) live in `store-icons/`.
