# F1 — Auditoria

Resultado: **aprovada** após duas correções.

- Entrega: reprovada pela trava (29 violações: comentários, pasta `scratch/`, evidência de compilação incompleta). Causa raiz: lições só de leitura não eram aplicadas; criada a trava `tools/verificar-regras.ps1` (L-14).
- Correção 1: trava em REGRAS OK; 37 testes; movimento para outro volume testado de verdade na unidade D:.
- Correção 2: `CaminhoEstendido` único com `Path.GetFullPath` e suporte a `\\?\UNC\` (L-15); localização por identidade devolve nulo nos dois formatos de ID (L-16). 44 testes passando, trava em REGRAS OK, script da trava intacto.
- Observação para a F2: parâmetros em inglês em `GerenciadorConfiguracao.Validar<T>` (L-12).

Tempo da desenvolvedora: 20 min (entrega), 35 min (correção 1), 91 min (correção 2).
