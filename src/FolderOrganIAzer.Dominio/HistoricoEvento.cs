using System;

namespace FolderOrganIAzer.Dominio;

public class HistoricoEvento
{
    public long Id { get; set; }
    public string ExecucaoId { get; set; } = string.Empty;
    public string OcorridoEm { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public long? ArquivoId { get; set; }
    public string? Origem { get; set; }
    public string? Destino { get; set; }
    public byte[]? Sha256 { get; set; }
    public string DetalhesJson { get; set; } = string.Empty;
    public byte[] HashEncadeado { get; set; } = Array.Empty<byte>();
}
