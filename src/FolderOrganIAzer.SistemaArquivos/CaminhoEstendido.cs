using System.IO;

namespace FolderOrganIAzer.SistemaArquivos;

public static class CaminhoEstendido
{
    public static string De(string caminho)
    {
        if (string.IsNullOrEmpty(caminho)) return caminho;
        if (caminho.StartsWith(@"\\?\")) return caminho;

        var fullPath = Path.GetFullPath(caminho);
        if (fullPath.StartsWith(@"\\"))
        {
            return @"\\?\UNC\" + fullPath.Substring(2);
        }
        return @"\\?\" + fullPath;
    }

    public static string RemoverPrefixo(string caminho)
    {
        if (caminho == null) return null!;
        if (caminho.StartsWith(@"\\?\UNC\")) return @"\\" + caminho.Substring(8);
        if (caminho.StartsWith(@"\\?\")) return caminho.Substring(4);
        return caminho;
    }
}
