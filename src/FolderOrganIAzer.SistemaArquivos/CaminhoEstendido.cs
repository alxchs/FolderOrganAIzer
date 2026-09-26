using System;
using System.IO;

namespace FolderOrganIAzer.SistemaArquivos;

public static class CaminhoEstendido
{
    public static string De(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            throw new ArgumentException("Caminho nulo ou vazio.", nameof(caminho));

        if (caminho.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase) ||
            caminho.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
        {
            return caminho;
        }

        string completo = Path.GetFullPath(caminho);

        if (completo.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
        {
            return @"\\?\UNC\" + completo[2..];
        }

        return @"\\?\" + completo;
    }
}
