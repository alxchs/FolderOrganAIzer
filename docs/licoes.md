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
| L-07 | Nada solto na raiz do repositório (a trava `tools\verificar-regras.ps1` lista o que é permitido). Verificação manual vira teste automatizado; arquivo de configuração novo precisa de justificativa no relatório. | F0, reincidente na F1 |
| L-08 | Cada camada é dona do seu recurso: migrações ficam embutidas na Persistência e o Host não lê SQL de disco. | F0 |
| L-09 | Um teste verifica um comportamento, com nome que o descreve. | F0 |
| L-10 | Relatório só com fatos verificáveis, sem adjetivos como "absoluto rigor". | F0 |
| L-11 | Antes de escrever "tudo feito", confira cada item da tarefa, um por um, contra o código, e cite no relatório onde cada item foi atendido. Item não cumprido declarado como cumprido é a falha mais grave. | F0, correção 1 |
| L-12 | Nome do arquivo igual ao da classe que ele contém, e nenhum identificador em inglês, nem variável local (`manager` deve ser `gerenciador`). | F0, correção 2 |
| L-13 | Teste que depende do ambiente é ignorado de verdade, nunca "passa" sem rodar: use um atributo próprio derivado de `FactAttribute` que preenche `Skip` com o motivo no construtor. | F1 |
| L-14 | Lições não bastam: rode a trava `tools\verificar-regras.ps1` antes de todo relatório. Entregar com violação na trava é entregar reprovado. | F1 |
| L-15 | Lógica repetida vira uma função única. Em especial, o prefixo `\\?\` desliga a normalização do Windows: todo caminho passa por uma única função que aplica `Path.GetFullPath` antes do prefixo. | F1 |
| L-16 | Um método tem uma única forma de dizer "não encontrado" (nulo ou resultado tipado), nunca nulo num ramo e exceção no outro. | F1 |
| L-17 | Classe que só os testes usam não está entregue. O código de produção precisa chamá-la: quem grava a transição consulta a máquina de estados; quem cria e mantém a execução usa a trava (batimento, zeragem do dono). | F2 |
| L-18 | Data gravada ou serializada segue a seção 5 da especificação (ISO 8601 UTC, milissegundos, `Z`), por uma única função de formatar e uma de ler. Nunca `ToString("o")`. | F2 |
| L-19 | Cada caso listado na tarefa vira um teste com o nome do caso. Se o ambiente não produz a situação (rede, FAT), injete a consulta por interface e teste com falso; o que não puder ser provado em disco real vai declarado no relatório, nunca omitido. | F2 |
| L-20 | Toda afirmação de comportamento no relatório ("zera o dono", "conduz o fluxo") cita arquivo e método, e o teste que a exercita. Afirmação sem código é afirmação falsa (reincidência de L-11). | F2, reincidente de L-11 |
