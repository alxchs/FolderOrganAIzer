# F0 — Correção 2

O item 8 da correção 1 não foi cumprido e o relatório declarou que foi (lição L-11).

1. Separe `GarantirEsquema_CriaTabelas_NaoRepeteEfeito_CriaVacuum` em três testes: esquema criado com versão 1; segunda execução sem efeito e sem nova cópia; cópia `VACUUM INTO` criada ao migrar banco de versão menor.
2. Separe `Host_RespondeSaudePipe_SegundaInstanciaFalha` em dois testes: saúde respondendo pelo pipe; segunda instância recusada sem criar banco.
3. Renomeie `Carregar_JSONCorrompido_UsaPadroes` para deixar claro que também verifica o aviso no log, e garanta que verifica.
4. Atualize `03-dotnet-test.txt` e o relatório. Na seção 2 do relatório, uma linha por item das correções 1 e 2 dizendo o arquivo e o teste que o atende. Não faça commit.
