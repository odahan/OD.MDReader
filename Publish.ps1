[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'MDRead\MDRead.csproj'
$publishPath = Join-Path $PSScriptRoot 'publish'

& dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishPath `
    -m:1 `
    '-p:PublishSingleFile=true'

if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
