# F0-relatorio.md

## 1. O que foi feito
1. **Remoção de comentários.** Os comentários presentes nos testes (`BancoTestes.cs` e `ConfiguracaoTestes.cs`) e classes de negócio foram removidos.
2. **Correção de nomes.** Nomenclaturas alteradas:
   - `AppConfig` para `ConfiguracaoAplicacao`.
   - `ConfiguracaoGerenciador` para `GerenciadorConfiguracao`.
   - `ChaveOcultaFormatter` para `FormatadorComChaveOculta`.
3. **Migrações e Banco de Dados.** O arquivo solto `schema.sql` foi excluído do disco e de referências de cópia no projeto. As migrações foram convertidas para recurso embutido em `Migracoes/001-esquema-inicial.sql` na camada de `Persistencia`. O método `GarantirEsquema()` sem parâmetros executa as pendentes em ordem numérica. O pacote `Microsoft.Data.Sqlite` foi atualizado para a versão `10.0.0`.
4. **Pasta de Dados do Host.** O executável do Host aceita a injeção da pasta de armazenamento através do argumento `--pasta-dados <caminho>`, operando com a pasta temporária durante os testes.
5. **Remoção do NuGet config.** O arquivo `nuget.config` foi excluído do repositório, pois a restauração de pacotes via `Directory.Packages.props` funciona na linha de comando local.

## 2. Critérios de aceite e evidências
- `mkfile d` e `mkfile r` compilam a solução sem erro nem aviso: Evidências `01-mkfile-d.txt` e `02-mkfile-r.txt`.
- `dotnet test FolderOrganIAzer.slnx` passa inteiro: Evidência `03-dotnet-test.txt`.
- Banco criado com `journal_mode` = `wal`, `synchronous` = `2` e `user_version` = `1`, lidos de volta por consulta: Arquivo `BancoTestes.cs`, teste `CriarConexao_AplicaPragmas`.
- `UPDATE` e `DELETE` em `historico` são recusados pelos gatilhos: Arquivo `BancoTestes.cs`, teste `Historico_GatilhosRecusamUpdateEDelete`.
- Nenhuma porta TCP aberta pelo host: Evidência `04-host-tcp.txt`.
- Arquivo de configuração ausente é criado com os padrões: Arquivo `Testes.cs`, teste `Carregar_ArquivoAusente_CriaComPadroes`.
- JSON corrompido usa os padrões e registra aviso: Arquivo `Testes.cs`, teste `Carregar_JSONCorrompido_UsaPadroesERegistraAviso`.
- Valores fora da faixa usa os padrões: Arquivo `Testes.cs`, teste `Carregar_ValoresForaDaFaixa_UsaPadroes`.
- Preço negativo em PrecosModelos é inválido: Arquivo `Testes.cs`, teste `Carregar_PrecoNegativo_Invalido`.
- Esquema criado com versão 1: Arquivo `BancoTestes.cs`, teste `GarantirEsquema_EsquemaCriadoComVersao1`.
- Segunda execução sem efeito e sem nova cópia: Arquivo `BancoTestes.cs`, teste `GarantirEsquema_SegundaExecucaoSemEfeitoESemCopia`.
- Cópia `VACUUM INTO` criada ao migrar banco de versão menor: Arquivo `BancoTestes.cs`, teste `GarantirEsquema_BancoMenorCriaVacuumInto`.
- `GET /v1/saude` responde pelo pipe nomeado: Arquivo `HostTestes.cs`, teste `Host_RespondeSaudePipe`.
- Segunda instância recusada sem criar banco: Arquivo `HostTestes.cs`, teste `Host_SegundaInstancia_FalhaSemCriarBanco`.

## 3. O que não foi feito ou não passou
Todos os requisitos da fase F0 e correções exigidas na Auditoria 1 e Auditoria 2 estão implementados e comprovados pelos testes.

## 4. Dúvidas para a arquiteta
Nenhuma.
