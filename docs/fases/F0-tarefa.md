# F0 — Fundação

Leia antes: `AGENTS.md`, `docs/licoes.md` e, na especificação, as seções 2, 4, 5, 15 (só a parte de transporte e `GET /v1/saude`), 16 (parâmetros) e 17 (F0).

Objetivo: o esqueleto que todas as fases usam. Nenhuma regra de negócio de organização de arquivos entra nesta fase.

## Itens

1. **Solução e projetos.** `FolderOrganIAzer.slnx` com todos os projetos listados no `AGENTS.md`, referências entre eles conforme a tabela "Projetos da solução" da seção 4, `Directory.Build.props` e `Directory.Packages.props`. Projetos ainda sem conteúdo ficam vazios (sem classes de exemplo).
2. **Banco (`FolderOrganIAzer.Persistencia`).**
    - Abre ou cria `%LOCALAPPDATA%\FolderOrganIAzer\dados\folderorganiazer.db`; o caminho da pasta de dados deve ser injetável para os testes usarem uma pasta temporária.
    - Aplica em toda conexão: `journal_mode=WAL`, `synchronous=FULL`, `foreign_keys=ON`, `busy_timeout=5000`.
    - Migrações versionadas por `PRAGMA user_version`. A migração 1 cria o esquema completo da seção 5, exatamente como está lá (todas as tabelas, índices e os dois gatilhos do histórico).
    - Antes de cada migração, se o banco já existir com versão menor, faz `VACUUM INTO` para uma cópia datada na pasta de dados. A migração roda numa transação única.
3. **Configuração.** `config.json` na pasta de dados com todos os parâmetros das tabelas da seção 16 e seus padrões. Validação das faixas. Arquivo ausente: cria com os padrões. Arquivo corrompido ou fora da faixa: usa os padrões, registra aviso no log e não para.
4. **Logs.** Serilog com arquivo diário em `%LOCALAPPDATA%\FolderOrganIAzer\logs\`, retenção de 30 dias, e o filtro que troca qualquer texto no formato `sk-ant-...` por `[CHAVE]`.
5. **Host (`FolderOrganIAzer.Host`).**
    - Mutex nomeado por usuário garantindo instância única: a segunda instância escreve uma mensagem no console e encerra com código diferente de zero, sem abrir o banco.
    - Kestrel escutando só no pipe nomeado `FolderOrganIAzer-<SID do usuário>`, sem nenhuma porta TCP.
    - Rota `GET /v1/saude` devolvendo JSON com versão do app, versão do esquema do banco e `bancoIntegro` (resultado de `PRAGMA quick_check`).
6. **Testes.** Unitários da validação de configuração e do filtro de chave. Integração: banco criado numa pasta temporária, PRAGMAs lidos de volta, gatilhos recusando `UPDATE` e `DELETE` em `historico`, migração repetida sem efeito, cópia `VACUUM INTO` criada ao migrar de versão menor.

## Critérios de aceite (todos com evidência real no relatório)

- [ ] `mkfile d` e `mkfile r` compilam a solução sem erro nem aviso.
- [ ] `dotnet test FolderOrganIAzer.slnx` passa inteiro.
- [ ] Banco criado com `journal_mode` = `wal`, `synchronous` = `2` e `user_version` = `1`, lidos de volta por consulta.
- [ ] `UPDATE` e `DELETE` em `historico` são recusados pelos gatilhos.
- [ ] Com o host rodando, uma segunda instância encerra com mensagem, sem abrir o banco.
- [ ] `GET /v1/saude` responde pelo pipe nomeado; mostre a chamada real feita por um cliente (por exemplo, um pequeno comando PowerShell com `NamedPipeClientStream`) e a resposta.
- [ ] Nenhuma porta TCP aberta pelo host (mostre `Get-NetTCPConnection` filtrado pelo PID do host).

Entregue `docs/fases/F0-relatorio.md` no formato do `AGENTS.md`. Não faça commit.
