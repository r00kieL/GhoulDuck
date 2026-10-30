param([string]$Compiler = $env:CSC_PATH)

$ErrorActionPreference = 'Stop'
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
    throw 'C# compiler not found. Specify -Compiler or set CSC_PATH.'
}

$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path ([System.IO.Path]::GetTempPath()) ('GhoulDuck.Tests.' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & $Compiler /nologo /target:exe "/out:$output" (Join-Path $root 'src\EnemyClassification.cs') (Join-Path $PSScriptRoot 'EnemyClassificationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $output
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
finally {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
}
