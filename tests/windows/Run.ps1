$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /codepage:65001 /out:"$workspace\work\CoreTests.exe" "$workspace\src\windows\Core.cs" "$workspace\src\windows\KeyboardState.cs" "$PSScriptRoot\CoreTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Core tests compilation failed.' }
& "$workspace\work\CoreTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
& $compiler /nologo /codepage:65001 /out:"$workspace\work\FeatureTests.exe" /reference:"$workspace\work\installer-payload\OzonMarkingConverter.exe" "$PSScriptRoot\FeatureTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Feature tests compilation failed.' }
Copy-Item -LiteralPath "$workspace\work\installer-payload\OzonMarkingConverter.exe" -Destination "$workspace\work\OzonMarkingConverter.exe" -Force
Copy-Item -LiteralPath "$workspace\src\windows\OzonMarkingConverter.exe.config" -Destination "$workspace\work\FeatureTests.exe.config" -Force
& "$workspace\work\FeatureTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Feature tests failed.' }
& $compiler /nologo /codepage:65001 /out:"$workspace\work\FieldValueTests.exe" /reference:"$workspace\work\installer-payload\OzonMarkingConverter.exe" "$PSScriptRoot\FieldValueTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Focused field regression tests compilation failed.' }
& "$workspace\work\FieldValueTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Focused field regression tests failed.' }
& $compiler /nologo /codepage:65001 /out:"$workspace\work\AltNumpadTests.exe" /reference:"$workspace\work\installer-payload\OzonMarkingConverter.exe" "$PSScriptRoot\AltNumpadTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Alt separator tests compilation failed.' }
& "$workspace\work\AltNumpadTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Alt separator tests failed.' }
