using System;

namespace FolderOrganIAzer.Dominio;

public class Raiz
{
    public long Id { get; set; }
    public string Caminho { get; set; } = string.Empty;
    public long VolumeSerial { get; set; }
    public byte[] FileId { get; set; } = Array.Empty<byte>();
    public string SistemaArquivos { get; set; } = string.Empty;
    public string CriadaEm { get; set; } = string.Empty;
}
