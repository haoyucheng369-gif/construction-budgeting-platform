# Dot-source this script: . ./scripts/Use-Dotnet.ps1
# Only the current PowerShell session is changed.
$ErrorActionPreference = 'Stop'
$localDotnetRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
$localDotnet = Join-Path $localDotnetRoot 'dotnet.exe'

if (Test-Path -LiteralPath $localDotnet) {
    $env:DOTNET_ROOT = $localDotnetRoot
    $env:PATH = "$localDotnetRoot;$env:PATH"
}

dotnet --version
if ($LASTEXITCODE -ne 0) {
    throw 'The SDK in global.json is unavailable. Install .NET SDK 8.0.425 or a compatible patch.'
}
