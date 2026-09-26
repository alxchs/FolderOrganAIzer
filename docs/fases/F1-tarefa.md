# F1 — Sistema de arquivos

Leia antes: `AGENTS.md`, `docs/licoes.md` (todas as lições, L-01 a L-12) e, na especificação, as seções 2, 5 (convenções de identidade), 7 (verificações 3 e 4), 8 (enumeração e prefixo `\\?\`), 12 (saneamento de nomes, marcador, criação pasta por pasta), 13 (protocolo de movimento e tabela de erros) e 17 (F1).

Objetivo: o projeto `FolderOrganIAzer.SistemaArquivos` com todas as operações de disco que as próximas fases usam. Nenhuma etapa do pipeline entra aqui.

## Itens

1. **Correção pendente da F0 (L-12):** renomeie `tests/FolderOrganIAzer.Testes.Unidade/Testes.cs` para o nome da classe que contém e troque a variável `manager` e qualquer outro identificador em inglês.
2. **Interop:** `Microsoft.Windows.CsWin32` com `NativeMethods.txt` listando só as APIs usadas. Todo caminho passado ao Win32 com prefixo `\\?\`.
3. **Identidade:** ler `FILE_ID_INFO` (volume de 64 bits + ID de 128 bits) de arquivo e de pasta, abrindo com `FILE_READ_ATTRIBUTES`, compartilhamento total e `FILE_FLAG_OPEN_REPARSE_POINT` (mais `FILE_FLAG_BACKUP_SEMANTICS` para pasta). Também tamanho, datas, atributos, número de vínculos físicos e etiqueta de ponto de reanálise.
4. **Volume:** tipo de unidade e sistema de arquivos (`NTFS`, `ReFS` ou outro) de um caminho.
5. **Criar pasta** com `CreateDirectoryW`, falhando se a pasta-mãe não existir. `Directory.CreateDirectory` é proibido no projeto.
6. **Mover arquivo** com `MoveFileExW(origem, destino, 0)`, traduzindo os códigos de erro da tabela da seção 13 para um resultado tipado (sem exceção genérica).
7. **Remover pasta vazia** com `RemoveDirectoryW`. Exclusão recursiva é proibida.
8. **Localizar arquivo pela identidade** com `OpenFileById` + `GetFinalPathNameByHandle`.
9. **Saneamento de nomes de pasta:** todas as regras da tabela da seção 12, como função pura em `FolderOrganIAzer.Dominio`.
10. **Marcador `.organizador`:** gravação atômica (temporário, `Flush(true)`, renomeia, depois atributos Oculto e Sistema) e leitura com validação do formato.
11. **Testes de integração** em pastas temporárias reais (nunca pastas do usuário), um comportamento por teste, incluindo: identidade igual antes e depois de mover no mesmo volume; `MoveFileExW` recusando sobrescrever; `CreateDirectoryW` falhando sem pasta-mãe; nome terminado em ponto acessível com `\\?\`; localizar arquivo movido pela identidade; marcador gravado com Oculto e Sistema e lido de volta. Testes unitários para cada regra de saneamento, incluindo `CON`, `..`, nome terminado em ponto e 61 caracteres.
12. **Recusa de volume para outro volume (erro 17):** teste que roda se existir uma segunda unidade local fixa com espaço, usando uma pasta temporária criada pelo próprio teste nela, e que é marcado como ignorado com o motivo quando não existir. FAT32 e exFAT ficam para verificação manual da auditoria; não crie VHDX.

## Regras de execução desta rodada

- Nunca deixe processo rodando em segundo plano (host, `dotnet run`, testes). Todo processo que você iniciar termina antes de você encerrar.
- Evidências (lições L-01 a L-03): `01-mkfile-r.txt`, `02-dotnet-test.txt` com `--list-tests` e a execução.

## Critérios de aceite

- [ ] `mkfile r` sem erro nem aviso.
- [ ] Todos os testes passam, incluindo os da F0.
- [ ] Cada item acima citado no relatório com o arquivo e o teste que o atende (L-11).

Não faça commit.
