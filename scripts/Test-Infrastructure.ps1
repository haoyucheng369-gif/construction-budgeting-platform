$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    docker compose config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Compose configuration is invalid.' }

    Get-Content -Raw -LiteralPath 'infra/postgres/verify.sql' |
        docker compose exec -T postgres sh -c 'export PGPASSWORD=$SALES_DB_PASSWORD; exec psql -h 127.0.0.1 -U sales_app -d vente --set ON_ERROR_STOP=1'
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL service ownership check failed.' }

    docker compose run --rm --no-deps -T --entrypoint /bin/bash sqlserver-init /init/verify-readonly.sh
    if ($LASTEXITCODE -ne 0) { throw 'SQL Server read-only check failed.' }

    $settings = @{}
    foreach ($line in Get-Content -LiteralPath '.env') {
        if ($line -match '^([A-Z_]+)=(.*)$') { $settings[$Matches[1]] = $Matches[2] }
    }
    $credentials = 'cb_app:{0}' -f $settings['RABBITMQ_PASSWORD']
    $header = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($credentials))
    $overview = Invoke-RestMethod 'http://127.0.0.1:15672/api/overview' -Headers @{ Authorization = "Basic $header" }
    Write-Host "RabbitMQ authenticated management request passed (version $($overview.rabbitmq_version))."
    Write-Host 'Infrastructure checks passed.'
}
finally {
    Pop-Location
}
