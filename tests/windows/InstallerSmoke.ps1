$ErrorActionPreference = 'Stop'
$workspace = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$testDirectory = [System.IO.Path]::GetFullPath((Join-Path $workspace 'work\installer-smoke'))
if (-not $testDirectory.StartsWith($workspace + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Test directory escapes workspace.' }
$testRegistry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PEI.OzonMarkingConverter.SetupTest_is1'
if (Test-Path -LiteralPath $testRegistry) { throw 'An existing test installation must be reviewed before testing.' }
if (Test-Path -LiteralPath $testDirectory) { throw 'Test directory already exists; inspect it before retrying.' }
$setup = Join-Path $workspace 'outputs\OzonMarkingConverter-Setup-Test.exe'
$arguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/DIR="' + $testDirectory + '"'),('/LOG="' + (Join-Path $workspace 'work\installer-smoke-install.log') + '"'))
$install = Start-Process -FilePath $setup -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($install.ExitCode -ne 0) { throw ('Installer failed: ' + $install.ExitCode) }
$exe = Join-Path $testDirectory 'OzonMarkingConverter.exe'
$uninstaller = Join-Path $testDirectory 'unins000.exe'
if (-not (Test-Path -LiteralPath $exe) -or -not (Test-Path -LiteralPath $uninstaller)) { throw 'Installed files missing.' }
foreach ($file in @('OzonMarkingConverter.exe.config','scanner.ico','Инструкция.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $testDirectory $file))) { throw ('Missing installed file: ' + $file) }
}
$entry = Get-ItemProperty -LiteralPath $testRegistry
$expectedVersion = [regex]::Match([IO.File]::ReadAllText((Join-Path $workspace 'src\windows\installer.iss')), '#define ProductVersion "([^"]+)"').Groups[1].Value
if ($entry.Publisher -ne 'by PEI' -or $entry.DisplayVersion -ne $expectedVersion) { throw 'Incorrect uninstall metadata.' }
$shell = New-Object -ComObject WScript.Shell
$shortcuts = @(
    (Join-Path $testDirectory '_test_shortcuts\Desktop\Преобразователь кода для Ozon.lnk'),
    (Join-Path $testDirectory '_test_shortcuts\StartMenu\Преобразователь кода для Ozon.lnk')
)
foreach ($shortcutPath in $shortcuts) {
    if (-not (Test-Path -LiteralPath $shortcutPath)) { throw ('Missing shortcut: ' + $shortcutPath) }
    $shortcut = $shell.CreateShortcut($shortcutPath)
    if ($shortcut.TargetPath -ne $exe) { throw 'Incorrect shortcut target.' }
    if ($shortcut.Description -notlike '*by PEI*') { throw 'Incorrect shortcut description.' }
}
Write-Output 'PASS: installation, EXE/config/icon, desktop/start-menu shortcut targets and by PEI publisher.'
$remove = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="' + (Join-Path $workspace 'work\installer-smoke-uninstall.log') + '"')) -WindowStyle Hidden -Wait -PassThru
if ($remove.ExitCode -ne 0) { throw ('Uninstaller failed: ' + $remove.ExitCode) }
for ($attempt = 0; $attempt -lt 20 -and (Test-Path -LiteralPath $exe); $attempt++) { Start-Sleep -Milliseconds 100 }
if (Test-Path -LiteralPath $exe) { throw 'Application EXE remains after uninstall.' }
if (Test-Path -LiteralPath $testRegistry) { throw 'Uninstall registration remains.' }
foreach ($shortcutPath in $shortcuts) { if (Test-Path -LiteralPath $shortcutPath) { throw 'Shortcut remains after uninstall.' } }
Write-Output 'PASS: uninstaller removed application, both shortcuts and test uninstall registration.'
