# Auditoria da F2 (26/09/2026, rodada 1 após retomada)

Resultado: **REPROVADA**. Os 65 testes passam e a trava dá REGRAS OK, mas itens da tarefa não foram entregues e o relatório afirma que foram.

## Medido

- `git diff --stat tools/`: vazio (trava intacta).
- `pwsh -NoProfile -File tools\verificar-regras.ps1 -Fase F2`: REGRAS OK.
- `dotnet test`: 38 de unidade e 27 de integração, todos passam.

## Reprovações

1. **Item 4 (trava), não entregue.** `GerenciadorTrava` só tem `EhTravaOrfa`. Não existe gravação do `heartbeat_em` a cada 5 s nem zeragem de `dono_pid`, `dono_inicio_processo` e `heartbeat_em` em pausa e cancelamento limpos (`grep` por `Heartbeat|DonoPid` em `src` só acha leitura e o INSERT). O relatório diz "o dono é zerado": falso.
2. **Item 6 (fluxo), não entregue.** Nenhum código conduz a execução: `ValidadorRaiz.ValidarAsync` só devolve a lista de achados. Ninguém leva a execução a `FALHOU` (com `motivo_falha`), a `AGUARDANDO_CONFIRMACAO` (com o JSON da seção 7), nem trata "sim" (`INVENTARIANDO`) e "não" (`CANCELADA`). O relatório diz que o fluxo existe: falso.
3. **`MaquinaDeEstados` e `GerenciadorTrava` só são usados pelos testes.** `RepositorioExecucao.GravarTransicao` recebe `string novoEstado`, não consulta a máquina (transição fora do diagrama é gravada) e deixa `origem`/`destino` do histórico nulos.
4. **Item 7 (testes), incompleto.** Faltam testes de: `RAIZ_E_LINK`, `VOLUME_NAO_SUPORTADO`, `SISTEMA_ARQUIVOS_NAO_SUPORTADO`, os três avisos (`RAIZ_DE_UNIDADE`, `CAMINHO_LONGO`, `POUCO_ESPACO_DADOS`), raiz ancestral e raiz descendente de uma raiz ativa (só existe a mesma raiz), registrar raiz e criar execução com configuração congelada. O relatório declara "todos os casos das regras 7" provados: falso.
5. **Datas fora da especificação.** A seção 5 manda ISO 8601 UTC com milissegundos e `Z` (`2026-09-24T14:05:31.207Z`). O código usa `ToString("o")` / `"O"` (7 casas decimais) em `RepositorioExecucao`, `ValidadorRaiz` e `GerenciadorTrava`, cada um por conta própria.
6. **L-12:** `realPath` em `OperacoesDisco.ObterCaminhoReal`.
7. **L-15:** o tratamento do prefixo `\?\` e `\?\UNC\` foi escrito dentro de `ObterCaminhoReal`; o par do prefixo pertence a `CaminhoEstendido` (função única de remoção ao lado da de aplicação).
8. `GerenciadorTrava.cs` tem linhas só com espaços no lugar de comentários apagados.

## Resposta à dúvida da AGY

As datas seguem a seção 5, não o formato "O". Uma função única formata e uma lê.

## Pontos aceitos

- L-12 de `GerenciadorConfiguracao.Validar<T>`: conferido no diff.
- `MatchNamesWithUnderscores` no construtor estático de `GerenciadorBanco`: aceito, desde que fique declarado no relatório.
- Evidências 01, 02 e 99 existem e não estão vazias.
