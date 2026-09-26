using System;
using System.IO;
using System.Text.Json;

namespace FolderOrganIAzer.SistemaArquivos;

public class MarcadorPasta
{
    public string Formato { get; set; } = "organizador-de-pastas/marcador";
    public int Versao { get; set; } = 1;
    public Guid Guid { get; set; }
    public int Nivel { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string? GranularidadeNivel2 { get; set; }
    public string? RotuloData { get; set; }
    public string RaizNaCriacao { get; set; } = string.Empty;
    public string ExecucaoId { get; set; } = string.Empty;
    public DateTimeOffset CriadaEm { get; set; }
}

public static class MarcadorOrganizador
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void GravarMarcador(string caminhoPasta, MarcadorPasta marcador)
    {
        string caminhoLongo = CaminhoEstendido.De(caminhoPasta);
        string caminhoArquivo = Path.Combine(caminhoLongo, ".organizador");
        string caminhoTemp = caminhoArquivo + ".tmp";

        string json = JsonSerializer.Serialize(marcador, JsonOptions);

        using (var fs = new FileStream(caminhoTemp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var writer = new StreamWriter(fs);
            writer.Write(json);
            writer.Flush();
            fs.Flush(true);
        }

        File.Move(caminhoTemp, caminhoArquivo, overwrite: true);
        
        File.SetAttributes(caminhoArquivo, FileAttributes.Hidden | FileAttributes.System);
    }

    public static MarcadorPasta? LerMarcador(string caminhoPasta)
    {
        string caminhoLongo = CaminhoEstendido.De(caminhoPasta);
        string caminhoArquivo = Path.Combine(caminhoLongo, ".organizador");

        if (!File.Exists(caminhoArquivo)) return null;

        try
        {
            string json = File.ReadAllText(caminhoArquivo);
            var marcador = JsonSerializer.Deserialize<MarcadorPasta>(json, JsonOptions);
            
            if (marcador != null && marcador.Formato == "organizador-de-pastas/marcador")
            {
                return marcador;
            }
        }
        catch (JsonException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }

        return null;
    }
}
