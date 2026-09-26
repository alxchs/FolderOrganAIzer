# F0 — Correção 1

Leia `docs/fases/F0-auditoria.md` e as lições novas em `docs/licoes.md`. Corrija tudo abaixo e reescreva `docs/fases/F0-relatorio.md` e a pasta `docs/fases/F0-evidencias/` do zero.

1. Remova todos os comentários do código, inclusive dos testes.
2. Renomeie: `AppConfig` para `ConfiguracaoAplicacao`, `ChaveOcultaFormatter` para `FormatadorComChaveOculta`, `ConfiguracaoGerenciador` para `GerenciadorConfiguracao`.
3. Migrações: scripts como recurso embutido em `FolderOrganIAzer.Persistencia` (`Migracoes/001-esquema-inicial.sql`), aplicados em ordem pelo número. `GarantirEsquema()` sem parâmetro aplica todas as pendentes. O Host não lê nenhum SQL de disco; remova o `schema.sql` solto e o item `Content`.
4. A pasta de dados do Host passa a ser informável por argumento (`--pasta-dados <caminho>`), com `%LOCALAPPDATA%\FolderOrganIAzer` como padrão.
5. Troque `test-pipe.ps1` por testes de integração que iniciam o executável do Host numa pasta de dados temporária e:
    - chamam `GET /v1/saude` pelo pipe nomeado com `HttpClient` (`SocketsHttpHandler.ConnectCallback` com `NamedPipeClientStream`) e verificam o JSON;
    - iniciam uma segunda instância com outra pasta de dados temporária e verificam código de saída diferente de zero e que nenhum `.db` foi criado nessa pasta.
6. Remova o `nuget.config`, ou justifique no relatório com o erro real que ele resolve.
7. Atualize `Microsoft.Data.Sqlite` para a versão estável mais recente da linha 10.x.
8. Separe o teste combinado em testes de um comportamento cada. Acrescente: arquivo de configuração ausente é criado com os padrões; JSON corrompido usa os padrões e registra aviso; preço negativo em `PrecosModelos` é inválido.
9. Evidências, cada uma não vazia, capturadas com `*>`:
    - `01-mkfile-d.txt`, `02-mkfile-r.txt`, `03-dotnet-test.txt` (com a lista de testes: `dotnet test --list-tests` e a execução);
    - `04-host-tcp.txt`: host iniciado pelo `.exe` Release, PID mostrado e `Get-NetTCPConnection -OwningProcess <PID>`.
10. Relatório só com fatos (L-10). Não faça commit.
