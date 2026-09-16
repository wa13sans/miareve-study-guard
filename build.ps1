$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found.' }
$buildDir = Join-Path $root 'build'
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
& $compiler /nologo /target:library /optimize+ "/out:$buildDir\ScreenAccessPlugin.dll" /r:System.Drawing.dll /r:System.Windows.Forms.dll "$root\src\ScreenAccessPlugin.cs"
if ($LASTEXITCODE -ne 0) { throw 'Plugin compilation failed' }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$root\MiareveStudyGuard.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll "/resource:$buildDir\ScreenAccessPlugin.dll,ScreenAccessPlugin.dll" "$root\src\App.cs" "$root\src\StudyPolicy.cs"
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed' }
& $compiler /nologo /target:exe "/out:$buildDir\PolicyTests.exe" "$root\src\StudyPolicy.cs" "$root\tests\PolicyTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& "$buildDir\PolicyTests.exe" "$buildDir\ScreenAccessPlugin.dll"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
Write-Host 'Built MiareveStudyGuard.exe — Beta 1.0.0 / Miareve'
