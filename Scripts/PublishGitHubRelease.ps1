<#
.SYNOPSIS
Publishes the freshly-built signed Release APK by committing it into
Journal/Releases/Latest/ in this repo. The in-app "Check for updates" feature reads
that folder's file listing via the GitHub Contents API and parses the build number out
of the filename (e.g. "Journal_1_0_6.apk" -> build 6) - it does NOT use GitHub Releases.

.EXAMPLE
dotnet publish Journal/Journal.csproj -f net10.0-android -c Release
powershell -File Scripts/PublishGitHubRelease.ps1
#>
param(
    [string]$ProjectPath = "$PSScriptRoot\..\Journal\Journal.csproj",
    [string]$RepoRoot = "$PSScriptRoot\..",
    [string]$LatestFolder = "Journal\Releases\Latest"
)

[xml]$xml = Get-Content $ProjectPath -Encoding UTF8
$versionNode = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationVersion } | Select-Object -First 1
if (-not $versionNode) {
    Write-Error "ApplicationVersion not found in $ProjectPath"
    exit 1
}

$appVersion = $versionNode.ApplicationVersion.Trim()

$publishDir = Join-Path (Split-Path $ProjectPath) "bin\Release\net10.0-android\publish"
$apk = Get-ChildItem -Path $publishDir -Filter "Journal_*.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $apk) {
    Write-Error "No Journal_*.apk found in $publishDir. Run 'dotnet publish -f net10.0-android -c Release' first."
    exit 1
}

$destFolder = Join-Path $RepoRoot $LatestFolder
if (-not (Test-Path $destFolder)) {
    New-Item -ItemType Directory -Path $destFolder -Force | Out-Null
}

Write-Host "Removing older APKs from $LatestFolder..." -ForegroundColor Yellow
Get-ChildItem -Path $destFolder -Filter "*.apk" -ErrorAction SilentlyContinue | Remove-Item -Force

$destPath = Join-Path $destFolder $apk.Name
Write-Host "Copying $($apk.Name) (build $appVersion) into $LatestFolder..." -ForegroundColor Yellow
Copy-Item -Path $apk.FullName -Destination $destPath -Force

Push-Location $RepoRoot
try {
    git add -- "$LatestFolder"
    git commit -m "Publish Journal build $appVersion"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git commit failed."
        exit 1
    }

    git push
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git push failed."
        exit 1
    }
}
finally {
    Pop-Location
}

Write-Host "Build $appVersion published to $LatestFolder and pushed." -ForegroundColor Green
exit 0
