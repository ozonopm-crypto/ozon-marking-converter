param([switch]$TestMode)
$ErrorActionPreference = 'Stop'
$workspacePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$payloadPath = Join-Path $workspacePath 'work\installer-payload'
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $payloadPath
$innoCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe')
)
$inno = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $inno) { throw 'Inno Setup compiler not found.' }
$compilerArgs = @('/Qp')
if ($TestMode) { $compilerArgs += '/DTestMode=1' }
$compilerArgs += (Join-Path $PSScriptRoot 'installer.iss')
& $inno @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
