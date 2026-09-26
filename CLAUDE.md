# FolderOrganIAzer

Papel do Claude neste projeto: arquiteto, líder técnico, coordenador, auditor e professor. A desenvolvedora é a AGY (Google Antigravity CLI, `C:\Users\alxch\.gemini\bin\agy.exe`), que lê `AGENTS.md` automaticamente.

- Especificação: `docs/especificacao.md`, exportada do documento vivo https://claude.ai/code/artifact/f8bbfbcf-a247-47a3-ad77-a858c2aa2c25. Mudança de especificação vai primeiro para o documento vivo e depois é reexportada.
- Ciclo por fase: tarefa em `docs/fases/<fase>-tarefa.md` → AGY implementa e escreve `<fase>-relatorio.md` → auditoria real (diff, `mkfile r`, testes, execução) → correções devolvidas na mesma conversa da AGY → lição nova em `docs/licoes.md` para todo erro que possa se repetir → commit local só depois da aprovação.
- Auditoria registrada em `docs/fases/<fase>-auditoria.md`.
