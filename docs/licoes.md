# Lições da auditoria

Cada lição nasce de um erro real encontrado na auditoria. Vale para todas as fases seguintes.

| ID | Lição | Origem |
| --- | --- | --- |
| L-01 | Antes de citar um arquivo de evidência, confira que ele existe e não está vazio. Evidência vazia ou ausente é critério não cumprido. | F0 |
| L-02 | No PowerShell, capture a saída com `*>` (todos os fluxos). `>` perde o que sai por `Write-Host`, como o `mkfile`. | F0 |
| L-03 | Evidência vem do artefato real (o `.exe` compilado), nunca de um intermediário como `dotnet run`, que tem processos e conexões próprios. | F0 |
| L-04 | Resultado inesperado nunca é explicado com palpite. Investigue até provar a causa, ou registre como não resolvido. | F0 |
| L-05 | Nenhum comentário em lugar nenhum, inclusive nos testes. | F0 |
| L-06 | Nomes só em português e consistentes entre si: `Gerenciador<Coisa>`, `Formatador<Coisa>`. Nada de `AppConfig`, `Formatter`. | F0 |
| L-07 | Nada solto na raiz do repositório. Verificação manual vira teste automatizado; arquivo de configuração novo precisa de justificativa no relatório. | F0 |
| L-08 | Cada camada é dona do seu recurso: migrações ficam embutidas na Persistência e o Host não lê SQL de disco. | F0 |
| L-09 | Um teste verifica um comportamento, com nome que o descreve. | F0 |
| L-10 | Relatório só com fatos verificáveis, sem adjetivos como "absoluto rigor". | F0 |
| L-11 | Antes de escrever "tudo feito", confira cada item da tarefa, um por um, contra o código, e cite no relatório onde cada item foi atendido. Item não cumprido declarado como cumprido é a falha mais grave. | F0, correção 1 |
| L-12 | Nome do arquivo igual ao da classe que ele contém, e nenhum identificador em inglês, nem variável local (`manager` deve ser `gerenciador`). | F0, correção 2 |
