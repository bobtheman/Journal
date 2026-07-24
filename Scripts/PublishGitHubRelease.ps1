<#
.SYNOPSIS
Publishes the freshly-built signed Release APK as a GitHub Release, tagged with the
app's ApplicationVersion. The in-app "Check for updates" feature compares its own
AppInfo.Current.BuildString against the latest release's tag on this repo.

.EXAMPLE
dotnet publish Journal/Journal.csproj -f net10.0-android -c Release
powershell -File Scripts/PublishGitHubRelease.ps1
#>
param(
    [string]$ProjectPath = "$PSScriptRoot\..\Journal\Journal.csproj",
    [string]$Repo = "bobtheman/Journal"
)

[xml]$xml = Get-Content $ProjectPath -Encoding UTF8
$versionNode = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationVersion } | Select-Object -First 1
if (-not $versionNode) {
    Write-Error "ApplicationVersion not found in $ProjectPath"
    exit 1
}

$appVersion = $versionNode.ApplicationVersion.Trim()
$displayVersion = $versionNode.ApplicationDisplayVersion.Trim()
$tag = "v$appVersion"

$publishDir = Join-Path (Split-Path $ProjectPath) "bin\Release\net10.0-android\publish"
$apk = Get-ChildItem -Path $publishDir -Filter "Journal_*.apk" -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $apk) {
    Write-Error "No Journal_*.apk found in $publishDir. Run 'dotnet publish -f net10.0-android -c Release' first."
    exit 1
}

Write-Host "Publishing $($apk.Name) as GitHub release $tag on $Repo..." -ForegroundColor Yellow

gh release create $tag $apk.FullName `
    --repo $Repo `
    --title "Journal $displayVersion (build $appVersion)" `
    --notes "Automated release for build $appVersion."

if ($LASTEXITCODE -ne 0) {
    Write-Error "gh release create failed."
    exit 1
}

Write-Host "Release $tag published." -ForegroundColor Green
exit 0
