# FolderOrganIAzer

Papel do Claude neste projeto: arquiteto, líder técnico, coordenador, auditor e professor. A desenvolvedora é a AGY (Google Antigravity CLI, `C:\Users\alxch\.gemini\bin\agy.exe`), que lê `AGENTS.md` automaticamente.

- Especificação: `docs/especificacao.md`, exportada do documento vivo https://claude.ai/code/artifact/f8bbfbcf-a247-47a3-ad77-a858c2aa2c25. Mudança de especificação vai primeiro para o documento vivo e depois é reexportada.
- Ciclo por fase: tarefa em `docs/fases/<fase>-tarefa.md` → AGY implementa e escreve `<fase>-relatorio.md` → auditoria real (diff, `mkfile r`, testes, execução) → correções devolvidas na mesma conversa da AGY → lição nova em `docs/licoes.md` para todo erro que possa se repetir → commit local só depois da aprovação.
- Auditoria registrada em `docs/fases/<fase>-auditoria.md`.
- Auditoria barata: primeiro `pwsh -NoProfile -File tools\verificar-regras.ps1 -Fase <fase>` e `dotnet test`; depois leitura manual só dos pontos de risco da fase. Confira que a trava não foi alterada (`git diff tools/`).
- Disparo da AGY (regra permanente já liberada): `C:\Users\alxch\.gemini\bin\agy.exe -p "<instrução>" --model gemini-3.1-pro-high --effort high --output-format json --print-timeout 60m`, em segundo plano, saída redirecionada para um `.json` no scratchpad. Correção vai na mesma conversa com `--conversation <id>`. Nunca agendar disparo com espera (bloqueado).
- Cota da AGY: erro 429 "Individual quota reached" informa quando renova. A troca automática de conta (`trocarConta`) e o redisparo na mesma conversa seguem a regra global "AGY: cota, troca de conta e papel do Claude" do `~/.claude/CLAUDE.md`.

## Estado atual

- F0 e F1 aprovadas e com commit.
- F2 incompleta, sem commit (26/09/2026 ~17:50): conversa AGY `12f8ee96-c773-4cbc-a4ab-615a03825cd9`, parada por cota (renova ~19:55). Estado medido: 38 testes de unidade passam; **9 de 27 testes de integração falham** (entre eles `ValidadorRaizTestes` de pasta gerada por marcador, por ancestral e execução inacabada); trava com 1 violação (`01-mkfile-r.txt` sem o resumo da compilação); `F2-relatorio.md` não existe.
- Próximo: retomar a conversa acima pedindo para corrigir os 9 testes, regravar evidências com os comandos exatos do AGENTS.md, trava em REGRAS OK e relatório; depois auditar e fazer a tarefa F3 (inventário, seção 8).
- Quedas "subscriber fell behind updates, stalled for 5s" ocorreram 2 vezes com saída redirecionada por `*>` no PowerShell; com redirecionamento pelo Bash (`> arquivo 2>&1`) a rodada seguinte não caiu (29 min, parou por cota). Dispare pelo Bash. Mas a queda voltou uma 3ª vez (26/09, 20:02) mesmo com Bash, ~14 min depois do disparo e com a AGY trabalhando; causa raiz não provada. Ao cair, confira `git status` (a AGY costuma ter avançado) e retome com `--conversation`, pedindo que ela confira o que já fez antes de refazer.
