# F1 — Correção 2

A trava passou, mas a revisão encontrou defeitos de desenho no núcleo de segurança (lições L-15 e L-16).

1. Crie um tipo único para caminho estendido (por exemplo `CaminhoEstendido.De(string)`) que aplica `Path.GetFullPath` e depois o prefixo `\\?\` (ou `\\?\UNC\` para caminho de rede), sem prefixar duas vezes. Substitua as 7 montagens manuais em `OperacoesDisco` e `MarcadorOrganizador`.
2. Testes unitários do tipo: caminho relativo, caminho com `..`, com `/`, já prefixado, e caminho de rede.
3. `LocalizarPorIdentidade`: extraia a leitura do caminho final para um método único usado pelos dois ramos, e devolva nulo quando o arquivo não for encontrado nos dois formatos de ID, sem exceção.
4. Teste: localizar a identidade de um arquivo apagado devolve nulo.
5. Evidências e trava como no `AGENTS.md`; relatório item por item. Não faça commit.
