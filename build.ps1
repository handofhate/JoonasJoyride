param(
    [string]$MoonSweepersGameRoot,
    [string]$DotnetPath,
    [switch]$CopyToRuntime
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'JoonasPassenger.csproj'
$legacyDotnet = Join-Path $root '..\Quantum Conveyors\work\dotnet-sdk\dotnet.exe'
if ([string]::IsNullOrWhiteSpace($DotnetPath)) {
    if (Test-Path -LiteralPath $legacyDotnet) { $DotnetPath = $legacyDotnet } else { $DotnetPath = 'dotnet' }
}
if ([string]::IsNullOrWhiteSpace($MoonSweepersGameRoot)) {
    $MoonSweepersGameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Moon Sweepers'
}

& $DotnetPath restore $project --force-evaluate --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $DotnetPath build $project -c Release --no-restore -p:MoonSweepersGameRoot=$MoonSweepersGameRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($CopyToRuntime) {
    $destination = 'C:\Program Files (x86)\Steam\steamapps\common\Moon Sweepers\BepInEx\plugins\JoonasJoyride'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'bin\Release\net6.0\JoonasJoyride.dll') -Destination $destination -Force
}
