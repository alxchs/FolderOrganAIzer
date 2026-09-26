using System;
using System.Globalization;

namespace FolderOrganIAzer.Dominio;

public static class FormatadorData
{
    private const string Formato = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public static string Formatar(DateTime data)
    {
        return data.ToUniversalTime().ToString(Formato, CultureInfo.InvariantCulture);
    }

    public static DateTime Ler(string dataTexto)
    {
        return DateTime.ParseExact(dataTexto, Formato, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
