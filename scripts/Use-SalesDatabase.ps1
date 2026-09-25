$ErrorActionPreference = 'Stop'
$salesEnvironmentFile = Join-Path (Split-Path -Parent $PSScriptRoot) '.env'
if (-not (Test-Path -LiteralPath $salesEnvironmentFile)) {
    throw 'Run scripts/Initialize-LocalEnvironment.ps1 first.'
}
$salesSettings = @{}
foreach ($salesLine in Get-Content -LiteralPath $salesEnvironmentFile) {
    if ($salesLine -match '^([A-Z_]+)=(.*)$') { $salesSettings[$Matches[1]] = $Matches[2] }
}
if ([string]::IsNullOrWhiteSpace($salesSettings['SALES_DB_PASSWORD'])) {
    throw 'SALES_DB_PASSWORD is missing from .env.'
}
# 为连接字符串中的密码值添加引号并转义内部引号，不显示密码。
$salesQuotedPassword = '"' + $salesSettings['SALES_DB_PASSWORD'].Replace('"', '""') + '"'
$env:ConnectionStrings__Sales = 'Host=127.0.0.1;Port=5432;Database=vente;Username=sales_app;Password=' + $salesQuotedPassword
Write-Host 'Sales connection configured for local Compose PostgreSQL; credentials are not displayed.'
