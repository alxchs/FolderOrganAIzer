param(
    [Parameter(Mandatory)] [string] $Fase
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent
$violacoes = [System.Collections.Generic.List[string]]::new()

$permitidosNaRaiz = @('.git', '.gitignore', '.gitattributes', 'AGENTS.md', 'CLAUDE.md', 'Directory.Build.props',
    'Directory.Packages.props', 'FolderOrganIAzer.slnx', 'nuget.config', 'docs', 'src', 'tests', 'tools')
Get-ChildItem $raiz -Force | Where-Object { $_.Name -notin $permitidosNaRaiz } |
    ForEach-Object { $violacoes.Add("Item solto na raiz: $($_.Name)") }

$fontes = Get-ChildItem (Join-Path $raiz 'src'), (Join-Path $raiz 'tests') -Recurse -Include *.cs, *.ps1 -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
$fontes | Select-String -Pattern '(^|\s)(//|/\*|#(?!region|endregion|if|else|endif|nullable|pragma))' |
    ForEach-Object { $violacoes.Add("Comentario: $($_.Path.Substring($raiz.Length + 1)):$($_.LineNumber)") }

$projetosDeOrganizacao = 'FolderOrganIAzer.SistemaArquivos', 'FolderOrganIAzer.Motor'
$projetosDeOrganizacao | ForEach-Object { Join-Path $raiz "src\$_" } | Where-Object { Test-Path $_ } |
    ForEach-Object { Get-ChildItem $_ -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } } |
    Select-String -Pattern 'Directory\.CreateDirectory|Directory\.Delete\(' |
    ForEach-Object { $violacoes.Add("API proibida: $($_.Path.Substring($raiz.Length + 1)):$($_.LineNumber)") }

$pastaEvidencias = Join-Path $raiz "docs\fases\$Fase-evidencias"
if (-not (Test-Path $pastaEvidencias)) {
    $violacoes.Add("Pasta de evidencias ausente: docs\fases\$Fase-evidencias")
}
else {
    $evidencias = Get-ChildItem $pastaEvidencias -File
    $evidencias | Where-Object Length -eq 0 | ForEach-Object { $violacoes.Add("Evidencia vazia: $($_.Name)") }
    $compilacao = $evidencias | Where-Object Name -match 'mkfile-r'
    if (-not $compilacao) { $violacoes.Add('Evidencia de mkfile r ausente') }
    elseif (-not (Select-String -Path $compilacao.FullName -Pattern '\b0 Warning\(s\)' -Quiet) -or
            -not (Select-String -Path $compilacao.FullName -Pattern '\b0 Error\(s\)' -Quiet)) {
        $violacoes.Add("Evidencia de mkfile r sem '0 Warning(s)' e '0 Error(s)': $($compilacao.Name)")
    }
    $testes = $evidencias | Where-Object Name -match 'dotnet-test'
    if (-not $testes) { $violacoes.Add('Evidencia de dotnet test ausente') }
    elseif ((Select-String -Path $testes.FullName -Pattern 'Failed!' -Quiet) -or
            -not (Select-String -Path $testes.FullName -Pattern 'Passed!' -Quiet)) {
        $violacoes.Add("Evidencia de dotnet test sem aprovacao completa: $($testes.Name)")
    }
}

if ($violacoes.Count -eq 0) {
    Write-Output "REGRAS OK para $Fase"
    exit 0
}
$violacoes | ForEach-Object { Write-Output "VIOLACAO: $_" }
Write-Output "TOTAL DE VIOLACOES: $($violacoes.Count)"
exit 1
