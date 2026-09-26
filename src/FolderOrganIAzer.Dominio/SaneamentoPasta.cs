using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FolderOrganIAzer.Dominio;

public static partial class SaneamentoPasta
{
    private static readonly string[] NomesReservados = 
    { 
        "CON", "PRN", "AUX", "NUL", 
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string SanearNome(string nome, bool removerAcentos)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return "_";
        }

        nome = nome.Normalize(NormalizationForm.FormC);

        if (removerAcentos)
        {
            nome = RemoverAcentos(nome);
        }

        nome = RegexProibidos().Replace(nome, " ");

        nome = RegexMultiplosEspacos().Replace(nome, " ");

        nome = nome.TrimStart(' ');
        while (nome.EndsWith(' ') || nome.EndsWith('.'))
        {
            nome = nome.TrimEnd(' ', '.');
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            nome = "_";
        }

        if (nome.Length > 60)
        {
            nome = nome[..60];
            while (nome.EndsWith(' ') || nome.EndsWith('.'))
            {
                nome = nome.TrimEnd(' ', '.');
            }
        }

        string nomeBase = nome;
        int dotIndex = nome.IndexOf('.');
        if (dotIndex >= 0)
        {
            nomeBase = nome[..dotIndex];
        }

        if (NomesReservados.Contains(nomeBase, StringComparer.OrdinalIgnoreCase))
        {
            nome += " _";
        }

        return nome;
    }

    private static string RemoverAcentos(string texto)
    {
        string formaD = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in formaD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[<>:""/\\|?*\x00-\x1F]")]
    private static partial Regex RegexProibidos();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RegexMultiplosEspacos();
}
