# 使用点调用方式加载脚本：. ./scripts/Use-Dotnet.ps1
# 仅修改当前 PowerShell 会话的环境。
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
