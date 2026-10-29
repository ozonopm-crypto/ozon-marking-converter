param(
    [Parameter(Mandatory=$true)][ValidatePattern('^\d{1,4}\.\d{1,4}\.\d{1,4}$')][string]$Version,
    [Parameter(Mandatory=$true)][string]$Notes
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$appPath = Join-Path $PSScriptRoot 'App.cs'
$app = [IO.File]::ReadAllText($appPath)
$match = [regex]::Match($app, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)')
if (-not $match.Success) { throw 'Current application version not found.' }
$previous = $match.Groups[1].Value
if ([version]$Version -lt [version]$previous) { throw 'A release must not downgrade the application.' }
[IO.File]::WriteAllText($appPath, $app.Replace($previous, $Version), [Text.UTF8Encoding]::new($true))
$setupPath = Join-Path $PSScriptRoot 'installer.iss'
$setup = [IO.File]::ReadAllText($setupPath)
$setup = [regex]::Replace($setup, '#define ProductVersion "[^"]+"', ('#define ProductVersion "' + $Version + '"'))
$setup = [regex]::Replace($setup, 'VersionInfoVersion=\d+\.\d+\.\d+\.0', ('VersionInfoVersion=' + $Version + '.0'))
[IO.File]::WriteAllText($setupPath, $setup, [Text.UTF8Encoding]::new($true))
& (Join-Path $PSScriptRoot 'build-installer.ps1')
& (Join-Path $workspace 'tests\windows\Run.ps1')
$exe = Join-Path $workspace ("outputs\OzonMarkingConverter-Setup-$Version.exe")
$publish = Join-Path $workspace 'outputs\publish'
New-Item -ItemType Directory -Path (Join-Path $publish 'downloads') -Force | Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $publish 'downloads') -Force
$feed = [ordered]@{
    version = $Version
    url = "https://raw.githubusercontent.com/ozonopm-crypto/ozon-marking-converter/main/downloads/OzonMarkingConverter-Setup-$Version.exe"
    sha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    size = (Get-Item -LiteralPath $exe).Length
    notes = $Notes
}
[IO.File]::WriteAllText((Join-Path $publish 'windows-updates.json'), ($feed | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
Write-Output "Release prepared: $publish"
