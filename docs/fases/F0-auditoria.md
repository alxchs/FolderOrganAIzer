# F0 — Auditoria 1

Resultado: **reprovada**. O código base funciona, mas há violações de regra, evidências inválidas e desenho de migração errado.

## Conferido pela auditoria (reproduzido de forma independente)

| Critério | Resultado real |
| --- | --- |
| `mkfile r` | Passa: 0 avisos, 0 erros. |
| `dotnet test` | 5 testes passam. Cobertura abaixo do pedido. |
| Esquema | Completo: 12 tabelas, 7 índices, 2 gatilhos, colunas acrescentadas depois presentes. |
| Host sem TCP | Correto: `FolderOrganIAzer.Host.exe` (Release) não abre nenhuma conexão TCP. |
| Segunda instância | Correto: sai com código 1. |

## Falhas

1. Evidência `01-mkfile-r.txt` vazia (0 bytes), citada como prova. O `mkfile` escreve pelo fluxo de informação do PowerShell; `>` só captura a saída padrão. Use `*>`.
2. Evidência dos PRAGMAs ausente (arquivo 03 inexistente).
3. Evidência de TCP medida no processo do `dotnet run`, não no host; a conexão encontrada foi explicada como "telemetria" sem nenhuma prova.
4. Comentários no código de teste (`BancoTestes.cs`, `ConfiguracaoTestes.cs`), proibidos pelo `AGENTS.md`.
5. Nomes fora do padrão: `AppConfig` (inglês), `ChaveOcultaFormatter` (misto), `ConfiguracaoGerenciador` inconsistente com `GerenciadorBanco`.
6. Migração carregada pelo `Host` a partir de `schema.sql` solto ao lado do executável e passada como parâmetro. Não há registro versionado de migrações.
7. `test-pipe.ps1` solto na raiz; `nuget.config` criado sem justificativa.
8. Teste único verificando três comportamentos; faltam testes de arquivo de configuração ausente, corrompido e de preço negativo.
9. `Microsoft.Data.Sqlite` 9.0.0 num projeto .NET 10, sem justificativa.
10. Relatório com adjetivos ("absoluto rigor") e declarando tudo pronto com evidência vazia.

# F0 — Auditoria 3 (após correções 1 e 2)

Resultado: **aprovada**, com duas observações que viram a lição L-12 e devem ser corrigidas na F1: arquivo `Testes.cs` contendo `GerenciadorConfiguracaoTestes`, e variável `manager`.

Reproduzido pela auditoria: `mkfile r` sem aviso nem erro; 12 testes passando (5 unidade, 7 integração), um comportamento por teste; host sem conexão TCP e segunda instância recusada; migrações embutidas na Persistência; nenhum comentário; raiz limpa.

Tempo da desenvolvedora: 12 min (entrega), 203 min (correção 1), 231 min (correção 2).
