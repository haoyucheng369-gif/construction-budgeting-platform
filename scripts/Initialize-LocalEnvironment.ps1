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
        # 为各账号生成不同的本地密码；不输出凭据，也不将其提交到版本库。
        '{0}=Cb!{1}' -f $Matches[1], [Guid]::NewGuid().ToString('N')
    }
    else {
        $line
    }
}
[System.IO.File]::WriteAllLines($environmentFile, [string[]]$lines, [System.Text.UTF8Encoding]::new($false))
Write-Host 'Created .env with generated local credentials. This file is excluded from Git.'
