$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot '.env'
if (Test-Path -LiteralPath $environmentFile) {
    Write-Host '.env already exists; existing configuration preserved.'
    return
}

$template = Get-Content -LiteralPath (Join-Path $repositoryRoot '.env.example')
$lines = foreach ($line in $template) {
    if ($line -match '^([A-Z_]+)=replace_with_local_password$') {
        # Distinct local passwords; no credentials are printed or committed.
        '{0}=Cb!{1}' -f $Matches[1], [Guid]::NewGuid().ToString('N')
    }
    else {
        $line
    }
}
[System.IO.File]::WriteAllLines($environmentFile, [string[]]$lines, [System.Text.UTF8Encoding]::new($false))
Write-Host 'Created .env with generated local credentials. This file is excluded from Git.'
