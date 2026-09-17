$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\build.ps1"
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testDir = Join-Path $PSScriptRoot 'build'
& $compiler /nologo /target:exe "/out:$testDir\IntegrationTests.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Net.Http.dll "/r:$PSScriptRoot\MiareveStudyGuard.exe" "$PSScriptRoot\tests\IntegrationTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Integration test compilation failed' }
Copy-Item -LiteralPath "$PSScriptRoot\MiareveStudyGuard.exe" -Destination "$testDir\MiareveStudyGuard.exe" -Force
& "$testDir\IntegrationTests.exe" "$testDir\test-artifacts" "$PSScriptRoot\MiareveAiBridge.exe"
if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed' }
