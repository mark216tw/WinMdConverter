[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "WinMdConverter.sln"
$artifactRoot = Join-Path $root "artifacts"
$portable = Join-Path $artifactRoot "portable"
$stageRoot = Join-Path $artifactRoot ".stage"
$appStage = Join-Path $stageRoot "app"
$cliStage = Join-Path $stageRoot "cli"
$zipPath = Join-Path $artifactRoot "WinMdConverter-$Version-$Runtime.zip"
$installerScript = Join-Path $root "installer\WinMdConverter.iss"

& (Join-Path $root "scripts\Generate-Icon.ps1")

if (Test-Path -LiteralPath $artifactRoot) {
    Remove-Item -LiteralPath $artifactRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $portable -Force | Out-Null
New-Item -ItemType Directory -Path $appStage -Force | Out-Null
New-Item -ItemType Directory -Path $cliStage -Force | Out-Null

if (-not $SkipTests) {
    & dotnet test $solution --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed."
    }
}

$publishProperties = @(
    "--configuration", "Release",
    "--runtime", $Runtime,
    "--self-contained", "true",
    "/p:Version=$Version",
    "/p:DebugType=None",
    "/p:DebugSymbols=false",
    "/p:PublishSingleFile=false",
    "/p:PublishTrimmed=false",
    "/p:SatelliteResourceLanguages=zh-Hant"
)

& dotnet publish (Join-Path $root "src\WinMdConverter.App\WinMdConverter.App.csproj") @publishProperties --output $appStage
if ($LASTEXITCODE -ne 0) {
    throw "GUI publish failed."
}

& dotnet publish (Join-Path $root "src\WinMdConverter.Cli\WinMdConverter.Cli.csproj") @publishProperties --output $cliStage
if ($LASTEXITCODE -ne 0) {
    throw "CLI publish failed."
}

Copy-Item -Path (Join-Path $appStage "*") -Destination $portable -Recurse -Force
Copy-Item -Path (Join-Path $cliStage "*") -Destination $portable -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination $portable -Force
Copy-Item -LiteralPath (Join-Path $root "LICENSE") -Destination $portable -Force
Copy-Item -LiteralPath (Join-Path $root "THIRD-PARTY-NOTICES.md") -Destination $portable -Force
Remove-Item -LiteralPath $stageRoot -Recurse -Force

Compress-Archive -Path (Join-Path $portable "*") -DestinationPath $zipPath -CompressionLevel Optimal

if (-not $SkipInstaller) {
    $isccCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    $iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($null -eq $iscc) {
        throw "Inno Setup 6 was not found. Use -SkipInstaller to create only the portable package."
    }

    $languageFile = Join-Path $artifactRoot "ChineseTraditional.isl"
    $languageUrl = "https://raw.githubusercontent.com/jrsoftware/issrc/main/Files/Languages/ChineseTraditional.isl"
    try {
        Invoke-WebRequest -Uri $languageUrl -OutFile $languageFile
        & $iscc "/DMyAppVersion=$Version" "/DMyLanguageFile=$languageFile" $installerScript
        $installerExitCode = $LASTEXITCODE
    }
    finally {
        if (Test-Path -LiteralPath $languageFile) {
            Remove-Item -LiteralPath $languageFile -Force
        }
    }

    if ($installerExitCode -ne 0) {
        throw "Installer build failed."
    }
}

$releaseFiles = @($zipPath)
$setupPath = Join-Path $artifactRoot "WinMdConverter-Setup-$Version-x64.exe"
if (Test-Path -LiteralPath $setupPath) {
    $releaseFiles += $setupPath
}
$checksums = $releaseFiles | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($_))"
}
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot "SHA256SUMS.txt"), $checksums)

Write-Host "Release artifacts created in $artifactRoot"
