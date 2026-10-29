param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build'))
$ErrorActionPreference = 'Stop'
$compilerRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $compilerRoot 'csc.exe'))) {
    $compilerRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}
$compiler = Join-Path $compilerRoot 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework compiler not found.' }
$outputFull = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputFull | Out-Null
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Management.dll','System.Web.Extensions.dll')
$arguments = @('/nologo','/codepage:65001','/target:winexe','/platform:anycpu','/optimize+',('/out:' + (Join-Path $outputFull 'OzonMarkingConverter.exe')),('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')))
$icon = Join-Path $PSScriptRoot 'scanner.ico'
if (Test-Path -LiteralPath $icon) { $arguments += '/win32icon:' + $icon }
foreach ($reference in $references) { $arguments += '/reference:' + $reference }
foreach ($reference in @('UIAutomationClient.dll','UIAutomationTypes.dll','WindowsBase.dll')) {
    $arguments += '/reference:' + (Join-Path (Join-Path $compilerRoot 'WPF') $reference)
}
$arguments += Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object { $_.FullName }
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'OzonMarkingConverter.exe.config') -Destination $outputFull -Force
if (Test-Path -LiteralPath $icon) { Copy-Item -LiteralPath $icon -Destination $outputFull -Force }
Write-Output ('Built: ' + (Join-Path $outputFull 'OzonMarkingConverter.exe'))
