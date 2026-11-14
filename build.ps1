param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw 'O compilador do .NET Framework 4.x não foi encontrado.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (('/resource:') + (Join-Path $PSScriptRoot 'assets\TranscreverTexto.png') + ',TranscreverTexto.png') (('/win32icon:') + (Join-Path $PSScriptRoot 'assets\SalvarCaptura.ico')) (('/out:') + (Join-Path $OutputDirectory 'SalvarCaptura.exe')) $sources
if ($LASTEXITCODE -ne 0) { throw 'A compilação falhou.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LEIA-ME.txt'),(Join-Path $PSScriptRoot 'Criar atalho.vbs') -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\SalvarCaptura.ico') -Destination $OutputDirectory -Force
Write-Output ('Versão final criada em: ' + $OutputDirectory)
