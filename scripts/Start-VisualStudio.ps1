$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot 'ConstructionBudgeting.sln'
$vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'

if (-not (Test-Path -LiteralPath $vswherePath)) {
    throw 'Visual Studio Installer / vswhere.exe was not found.'
}

. (Join-Path $PSScriptRoot 'Use-Dotnet.ps1')

$sdkVersion = & (Join-Path $env:DOTNET_ROOT 'dotnet.exe') --version
if ($LASTEXITCODE -ne 0) {
    throw 'The project .NET SDK could not be resolved.'
}

$sdkDirectory = Join-Path $env:DOTNET_ROOT "sdk\$sdkVersion"
if (-not (Test-Path -LiteralPath (Join-Path $sdkDirectory 'Sdks'))) {
    throw "The .NET SDK directory does not exist: $sdkDirectory"
}

$vsInstallPath = & $vswherePath -latest -products Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -property installationPath | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($vsInstallPath)) {
    throw 'Visual Studio was not found.'
}

$devenvPath = Join-Path $vsInstallPath 'Common7\IDE\devenv.exe'
if (-not (Test-Path -LiteralPath $devenvPath)) {
    throw "Visual Studio executable does not exist: $devenvPath"
}

# 仅为此次启动的 Visual Studio 指定用户目录中的 SDK。
$env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = Join-Path $sdkDirectory 'Sdks'
$env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_VER = $sdkVersion
$env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $env:DOTNET_ROOT

# F5 启动 Sales API 时沿用本地数据库连接，不在输出中显示密码。
. (Join-Path $PSScriptRoot 'Use-SalesDatabase.ps1')

$visualStudio = Start-Process -FilePath $devenvPath -ArgumentList ('"{0}"' -f $solutionPath) -PassThru
Write-Host "Visual Studio started with .NET SDK $sdkVersion (PID $($visualStudio.Id))."
