# F1 — Correção 1

A trava `tools\verificar-regras.ps1 -Fase F1` encontrou 29 violações na entrega. Leia as lições L-07, L-13 e L-14 e as seções novas do `AGENTS.md` ("Trava obrigatória" e "Ambiente desta máquina").

1. Remova todos os comentários de `src` e `tests`. Os códigos de erro Win32 viram constantes nomeadas (`ErroCompartilhamento = 32`...), não comentários.
2. Apague a pasta `scratch/` que você criou.
3. Reescreva o `nuget.config` na forma padrão do `AGENTS.md`.
4. O teste de movimento para outro volume passa a usar um atributo próprio (L-13) que o marca como ignorado, com o motivo, quando não houver segunda unidade fixa.
5. `SaneamentoPasta`: nome reservado recebe o sufixo ` _` (espaço e sublinhado), exatamente como na seção 12. Documente no relatório o que acontece quando o nome truncado termina em ponto.
6. Leituras e escritas do marcador também com o prefixo `\\?\`.
7. Grave as evidências com os comandos exatos do `AGENTS.md`, rode a trava até `REGRAS OK` e grave `99-verificar-regras.txt`. Reescreva o relatório citando item por item. Não faça commit.
