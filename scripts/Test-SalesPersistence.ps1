$ErrorActionPreference = 'Stop'
$salesPreviousConnection = $env:ConnectionStrings__Sales
$salesPreviousTestFlag = $env:SALES_PERSISTENCE_TESTS
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    . ./scripts/Use-Dotnet.ps1
    . ./scripts/Use-SalesDatabase.ps1
    $env:SALES_PERSISTENCE_TESTS = '1'
    dotnet restore ConstructionBudgeting.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }
    dotnet build ConstructionBudgeting.sln --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet test ConstructionBudgeting.sln --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
finally {
    $env:ConnectionStrings__Sales = $salesPreviousConnection
    $env:SALES_PERSISTENCE_TESTS = $salesPreviousTestFlag
    Pop-Location
}
