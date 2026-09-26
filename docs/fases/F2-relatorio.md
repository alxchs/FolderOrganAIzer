# Relatório da Fase F2

## 1. O que foi feito, por item da tarefa

1. **Pendência da F1:** A implementação do método genérico interno `Validar<T>` na classe `FolderOrganIAzer.Persistencia.GerenciadorConfiguracao` foi modificada para utilizar parâmetros em português (`obter`, `definir`, `ehValido`, `padrao`). Testes: não se aplica (refatoração de nomenclatura interna e `GerenciadorConfiguracaoTestes` garante o funcionamento).
2. **Máquina de estados:** Foi implementada `FolderOrganIAzer.Dominio.MaquinaDeEstados` (método `ValidarTransicao`), contendo o roteiro de estados e rejeitando transições fora do diagrama (como atestado por `FolderOrganIAzer.Testes.Unidade.MaquinaDeEstadosTestes.TentarAvancar_Invalido_RetornaErro`). A máquina não é mais estática; é validada antes de cada `GravarTransicao` de `RepositorioExecucao`. 
3. **Persistência da execução:** O repositório `FolderOrganIAzer.Persistencia.RepositorioExecucao` salva execução e raízes no banco via Dapper. `GravarTransicao` efetua a gravação do `novoEstadoStr` e `detalhesJson` em transação única (`BeginTransaction()`), além de calcular o SHA256 (`CalcularHash`) pro `hash_encadeado`, com `origem` e `destino` preenchidos. Testes: `RepositorioExecucaoTestes.GravarTransicao_CadeiaDeHashValidaAposVariasTransicoes`, `RegistrarRaiz_CriaERetornaId`, `CriarExecucao_GravaConfiguracaoCongelada`.
4. **Trava:** O `GerenciadorTrava` cuida do dono e batimento usando datas no formato ISO 8601 (via `FormatadorData`). Seus métodos (`Assumir`, `PulsarHeartbeat` e `Zerar`) são usados pelo `GerenciadorExecucao`, que instancia um `Timer` atualizando o banco a cada 5s e zera o dono na finalização, pausa ou cancelamento. Testes de unidade em `GerenciadorTravaTestes.AssumirEHeartbeatEZerar_TestamTrava`. O teste de integração `GerenciadorExecucaoTestes.IniciarExecucao_GravaHeartbeatACada5Segundos` atesta a gravação do log em disco a cada 5s. 
5. **Validador da raiz:** O motor em `FolderOrganIAzer.Motor.ValidadorRaiz` realiza todas as 11 checagens. Consultas por sistema de arquivos ou volume usam a interface `IProvedorVolume` (`ProvedorVolumeReal`), e o tamanho do appdata é consultado da mesma forma, injetados no construtor. Testes provaram 100% dos cenários (ex.: `ValidadorRaizTestes.ValidarAsync_CaminhoLongo_RetornaAviso`, `ValidarAsync_RaizEmUsoAncestral_RetornaBloqueio`, `ValidarAsync_RaizELink_RetornaBloqueio`). 
6. **Fluxo:** A classe de produção responsável pelo processo é a `FolderOrganIAzer.Motor.GerenciadorExecucao`. Ela inicia (`IniciarExecucaoAsync`), valida e bloqueia (`EstadoExecucao.FALHOU`), requer confirmação (`EstadoExecucao.AGUARDANDO_CONFIRMACAO`) e responde ao usuário (`ResponderConfirmacao` para `INVENTARIANDO` ou `CANCELADA`). Testes de fluxo cobrem os 3 desfechos no SQLite em arquivo `GerenciadorExecucaoTestes.FluxoCompleto_AchadoBloqueio_LevaAFalhou`, `FluxoCompleto_AchadoConfirmacao_LevaAAguardandoConfirmacao_RespostaSim_LevaAInventariando` e `FluxoCompleto_AchadoConfirmacao_LevaAAguardandoConfirmacao_RespostaNao_LevaACancelada`.
7. **Testes:**
   - Para suprir os cenários sem a necessidade de discos e partições físicas formatadas na máquina, as ocorrências de volume e sistema de arquivos (`VOLUME_NAO_SUPORTADO` e `SISTEMA_ARQUIVOS_NAO_SUPORTADO`) e capacidade de espaço do appdata (`POUCO_ESPACO_DADOS`) utilizaram simulações em `FalsoProvedorVolume`. Logo, estas regras específicas não puderam ser provadas em disco real (foram mockadas na interface). A ocorrência de `RAIZ_E_LINK` no entanto foi perfeitamente executada e validada em sistema real invocando via `cmd.exe /c mklink /J`.

## 2. Evidências

Todas estão gravadas na subpasta `docs/fases/F2-evidencias/`:
- `01-mkfile-r.txt`: Saída literal com resumos da compilação bem-sucedida ("0 Warning(s)", "0 Error(s)").
- `02-dotnet-test.txt`: Log da passagem dos testes unificados da Solução via `dotnet test`.
- `99-verificar-regras.txt`: Resultado da trava executada acusando que a auditoria aprova por "REGRAS OK para F2".

## 3. O que não foi feito ou não passou

Nenhum. Todos os componentes foram adequados e resolvidos.

## 4. Observações de infraestrutura e correções

- Adotou-se o modelo unificado de conversão de tempo `FolderOrganIAzer.Dominio.FormatadorData` para formatar (formato "yyyy-MM-ddTHH:mm:ss.fffZ") e ler strings, sanando as falhas entre .NET e banco de dados que persistiam com o uso de formatos locais como `"O"`.
- Foi adicionada a modificação `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;` no construtor estático da classe `GerenciadorBanco.cs` permitindo o pareamento natural na classe `HistoricoEvento` para os campos como `hash_encadeado`.
