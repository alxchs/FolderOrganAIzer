# FolderOrganIAzer — regras para a desenvolvedora (AGY)

Você é a desenvolvedora deste projeto. A arquiteta, líder técnica e auditora é o Claude, que escreve as tarefas, audita cada entrega e registra as lições. O dono do produto é o Alexandre.

Leia este arquivo inteiro e também `docs/licoes.md` antes de qualquer trabalho. As lições valem tanto quanto estas regras.

## Fonte da verdade

- `docs/especificacao.md` é a especificação completa. Não a edite.
- A tarefa da vez está em `docs/fases/<fase>-tarefa.md`. Faça só o que ela pede, nada de fases futuras.
- Se a especificação estiver errada, ambígua ou contraditória, não invente: registre a dúvida no relatório, na seção "Dúvidas para a arquiteta", e siga com o restante.
- Onde a especificação diz `Organizador` ou `OrganizadorDePastas` (nomes de projeto, executável, pasta de dados, pipe, alvo de credencial), use `FolderOrganIAzer`.

## Stack

- C# em .NET 10 (`net10.0-windows10.0.19041.0` onde houver API do Windows; `net10.0` no domínio puro).
- Solução `FolderOrganIAzer.slnx` na raiz do repositório; projetos em `src/`, testes em `tests/`.
- Projetos: `FolderOrganIAzer.Dominio`, `.Persistencia`, `.SistemaArquivos`, `.Extracao`, `.Extrator` (executável), `.Classificacao`, `.Motor`, `.Host` (executável). Testes: `FolderOrganIAzer.Testes.Unidade`, `FolderOrganIAzer.Testes.Integracao`.
- `Directory.Build.props` com `Nullable` habilitado, `TreatWarningsAsErrors` verdadeiro e `ImplicitUsings` habilitado. Versões de pacote centralizadas em `Directory.Packages.props`.
- Testes com xUnit e `Assert` nativo. Não use FluentAssertions (licença comercial).
- Só as bibliotecas da seção 4 da especificação. Biblioteca nova exige justificativa no relatório.

## Padrão de código (inegociável)

- Nenhum comentário no código, de nenhum tipo. O código se explica por nomes.
- Código limpo, escrito para um revisor profissional exigente: métodos curtos, uma responsabilidade por classe, nomes completos em português conforme o domínio da especificação, sem abreviações.
- Sem código morto, sem `TODO`, sem arquivos de exemplo que sobraram de templates.
- Nomes de tipos, métodos e propriedades em português, seguindo os termos da especificação (Execucao, Arquivo, PastaPlanejada, Movimento, Historico...).

## Compilar e testar

- Compile com `mkfile d` (desenvolvimento) e `mkfile r` (release), na raiz do repositório. Não chame o compilador de outro jeito para declarar que compila.
- Rode os testes com `dotnet test FolderOrganIAzer.slnx`.
- Uma fase só está pronta com `mkfile r` sem erro, todos os testes passando e a funcionalidade executada de verdade.

## Segurança do ambiente (inegociável)

- Nunca leia, crie, altere, mova ou apague nada fora do repositório, exceto: pastas temporárias criadas pelos próprios testes (em `%TEMP%`) e `%LOCALAPPDATA%\FolderOrganIAzer` durante execuções manuais.
- Nunca aponte o app, um teste ou um comando para pastas reais do usuário (Downloads, Documentos, Desktop, `D:\drived`, OneDrive).
- Nunca faça `git commit`, `git push`, `git reset` ou `git checkout` de arquivos. A arquiteta faz os commits depois da auditoria.
- Nunca apague arquivos que você não criou nesta tarefa.

## Relatório de entrega

Ao terminar, escreva `docs/fases/<fase>-relatorio.md` com:

1. O que foi feito, por item da tarefa.
2. Cada critério de aceite da tarefa com a **evidência real**: o comando exato executado e a saída real copiada (resumida só nas partes repetitivas). "Deve funcionar" ou "foi implementado" não é evidência.
3. O que não foi feito ou não passou, dito claramente.
4. Dúvidas para a arquiteta.

Declarar algo pronto sem evidência é a falha mais grave possível neste projeto.
