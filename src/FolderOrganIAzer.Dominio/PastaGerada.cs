using System;

namespace FolderOrganIAzer.Dominio;

public class PastaGerada
{
    public long Id { get; set; }
    public string ExecucaoId { get; set; } = string.Empty;
    public string RaizCaminho { get; set; } = string.Empty;
    public string CaminhoRelativo { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public long VolumeSerial { get; set; }
    public byte[] FileId { get; set; } = Array.Empty<byte>();
    public string CriadaEm { get; set; } = string.Empty;
    public string? RemovidaEm { get; set; }
}
