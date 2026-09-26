# F2 — Validação da pasta, máquina de estados e trava

Leia antes: `AGENTS.md`, `docs/licoes.md` (L-01 a L-16) e, na especificação, as seções 2 (R-05, R-13), 5 (tabelas `raiz`, `execucao`, `historico`), 6 (máquina de estados, trava e detecção de desligamento anormal), 7 (inteira), 12 (marcador) e 17 (F2).

Objetivo: criar uma execução para uma raiz, validá-la pelas 11 verificações da seção 7 e conduzi-la pela máquina de estados persistida até `INVENTARIANDO`, `AGUARDANDO_CONFIRMACAO`, `CANCELADA` ou `FALHOU`. Inventário e etapas seguintes não entram.

## Itens

1. **Pendência da F1 (L-12):** parâmetros `get`, `set`, `isValid` de `GerenciadorConfiguracao.Validar<T>` em português.
2. **Máquina de estados** em `FolderOrganIAzer.Dominio`: todos os estados da tabela `execucao` e as transições do diagrama da seção 6 (inclusive `AGUARDANDO_CUSTO`), mais `PAUSADA`, `CANCELADA` e `FALHOU` a partir de qualquer estado não final. Transição fora do diagrama é recusada pelo domínio com resultado tipado.
3. **Persistência da execução** em `FolderOrganIAzer.Persistencia`: registrar raiz (pela identidade), criar execução com configuração congelada, gravar cada transição e o evento `ESTADO_ALTERADO` no histórico **na mesma transação**, com o `hash_encadeado` da seção 14.
4. **Trava** (seção 6): `dono_pid`, `dono_inicio_processo`, `heartbeat_em` a cada 5 s; execução órfã quando o PID não existe, existe com outra hora de início ou o batimento está parado há mais de 60 s. Pausa e cancelamento limpos zeram o dono.
5. **Validador da raiz** em `FolderOrganIAzer.Motor`, com as 11 verificações da seção 7, na ordem, cada uma produzindo o código e a severidade da tabela; mais os avisos adicionais e a lista de locais protegidos. A verificação 7 usa `CreateDirectoryW` e `RemoveDirectoryW` da F1 numa pasta oculta `.organizador-teste-<guid>`. A verificação 8 usa os três sinais (marcador, registro, ancestral até a avó). A verificação 11 recebe o provedor de IA por interface, para os testes usarem um falso.
6. **Fluxo:** achado de bloqueio leva a `FALHOU` com o motivo; achado de confirmação leva a `AGUARDANDO_CONFIRMACAO` com os dados do JSON de exemplo da seção 7; resposta "sim" leva a `INVENTARIANDO`, "não" a `CANCELADA`.
7. **Testes** com pastas temporárias reais, um comportamento por teste:
    - cada código de achado da seção 7 produzido por uma situação real;
    - pasta gerada detectada por cada sinal isoladamente: marcador presente; marcador apagado mas identidade registrada; pasta criada pelo usuário dentro de uma pasta gerada;
    - transição inválida recusada;
    - execução em raiz ancestral e em raiz descendente de uma raiz ativa recusada;
    - trava órfã reconhecida quando o PID gravado não existe e quando a hora de início não confere;
    - cadeia de hash do histórico válida após várias transições.
    Para `VOLUME_NAO_SUPORTADO`, `SISTEMA_ARQUIVOS_NAO_SUPORTADO` e `LOCAL_PROTEGIDO`, teste com caminhos que já existem nesta máquina (por exemplo `C:\Windows`), só lendo; nunca crie nada neles.

## Regras de execução

- Nunca deixe processo em segundo plano.
- Evidências e trava como no `AGENTS.md`; relatório item por item (L-11). Não faça commit.
