param(
    [string]$DuckovPath = $env:DUCKOV_PATH,
    [string]$HarmonyPath = $env:HARMONY_PATH,
    [string]$Compiler = $env:CSC_PATH
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($DuckovPath)) {
    throw 'Specify -DuckovPath or set DUCKOV_PATH to the game installation directory.'
}
$managed = Join-Path $DuckovPath 'Duckov_Data\Managed'
if (!(Test-Path (Join-Path $managed 'TeamSoda.Duckov.Core.dll'))) {
    throw "Game assemblies not found: $managed"
}
if ([string]::IsNullOrWhiteSpace($HarmonyPath)) {
    $steamApps = Split-Path (Split-Path $DuckovPath -Parent) -Parent
    $HarmonyPath = Join-Path $steamApps 'workshop\content\3167020\3589088839\0Harmony.dll'
}
if (!(Test-Path $HarmonyPath)) {
    throw "Harmony DLL not found: $HarmonyPath. Specify -HarmonyPath or set HARMONY_PATH."
}
if ([string]::IsNullOrWhiteSpace($Compiler)) {
    $csc = Get-Command 'csc.exe' -ErrorAction SilentlyContinue
    if ($csc) { $Compiler = $csc.Source }
    if (!$Compiler -and ${env:ProgramFiles(x86)}) {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path $vswhere) {
            $Compiler = (& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1)
        }
    }
}
if ([string]::IsNullOrWhiteSpace($Compiler) -or !(Test-Path $Compiler)) {
    throw 'C# compiler not found. Install Visual Studio Build Tools, specify -Compiler, or set CSC_PATH.'
}

$outDir = Join-Path $PSScriptRoot 'dist\GhoulDuck'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$refs = @('mscorlib.dll','netstandard.dll','System.dll','System.Core.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.JSONSerializeModule.dll','UnityEngine.PhysicsModule.dll','UnityEngine.UI.dll','UnityEngine.UIModule.dll','Unity.InputSystem.dll','TeamSoda.Duckov.Core.dll','TeamSoda.Duckov.Utilities.dll','ItemStatsSystem.dll','Cysharp.Threading.Tasks.dll','SodaLocalization.dll')
$compilerArgs = @('/nologo','/target:library','/nostdlib+','/langversion:latest','/utf8output',('/out:' + (Join-Path $outDir 'GhoulDuck.dll')))
foreach ($name in $refs) {
    $path = Join-Path $managed $name
    if (Test-Path $path) { $compilerArgs += '/reference:' + $path }
}
$compilerArgs += '/reference:' + $HarmonyPath
$compilerArgs += Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | ForEach-Object { $_.FullName }
& $Compiler $compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }

Copy-Item -LiteralPath $HarmonyPath -Destination (Join-Path $outDir '0Harmony.dll') -Force
$infoPath = Join-Path $outDir 'info.ini'
$publishedId = $null
if (Test-Path -LiteralPath $infoPath) {
    foreach ($line in Get-Content -LiteralPath $infoPath) {
        if ($line -match '^\s*publishedFileId\s*=\s*([1-9][0-9]*)\s*$') {
            $publishedId = $Matches[1]
            break
        }
    }
}
$template = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'package\info.ini') -Raw
if ($publishedId) {
    $idPattern = [regex]::new('(?m)^[ \t]*publishedFileId[ \t]*=[^\r\n]*')
    if ($idPattern.Matches($template).Count -gt 1) { throw 'The source info.ini contains duplicate publishedFileId entries.' }
    if ($idPattern.IsMatch($template)) {
        $template = $idPattern.Replace($template, "publishedFileId=$publishedId", 1)
    } else {
        $template = $template.TrimEnd("`r", "`n") + "`n" + "publishedFileId=$publishedId`n"
    }
}
[System.IO.File]::WriteAllText($infoPath, $template, [System.Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'package\Harmony-LICENSE.txt') -Destination (Join-Path $outDir 'Harmony-LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination (Join-Path $outDir 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'images\preview.png') -Destination (Join-Path $outDir 'preview.png') -Force
Write-Host "Built $outDir"
