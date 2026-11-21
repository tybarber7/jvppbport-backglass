$ErrorActionPreference = 'Stop'

$framework = Join-Path $env:WINDIR 'Microsoft.NET'
$compiler = @(
    (Join-Path $framework 'FrameworkArm64/v4.0.30319/csc.exe')
    (Join-Path $framework 'Framework64/v4.0.30319/csc.exe')
    (Join-Path $framework 'Framework/v4.0.30319/csc.exe')
) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1

if (-not $compiler) {
    throw 'The .NET Framework 4.x C# compiler was not found on this Windows PC.'
}

$destination = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$binary = Join-Path $destination 'CabinetBackglass.exe'
$files = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File |
    Sort-Object Name | ForEach-Object FullName
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$binary" $files
if ($LASTEXITCODE -ne 0) { throw "C# build failed with exit code $LASTEXITCODE." }

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/Panoramas') -Destination $destination -Recurse -Force
Write-Output "Built $binary"
