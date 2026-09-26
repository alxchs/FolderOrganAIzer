using System;

namespace FolderOrganIAzer.Dominio;

public class Execucao
{
    public string Id { get; set; } = string.Empty;
    public long RaizId { get; set; }
    public EstadoExecucao Estado { get; set; }
    public EstadoExecucao? EstadoAntesPausa { get; set; }
    public string? ConfirmadaPeloUsuarioEm { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
    public string VersaoApp { get; set; } = string.Empty;
    public int VersaoTaxonomia { get; set; }
    public int VersaoPrompt { get; set; }
    
    
    public int? DonoPid { get; set; }
    public string? DonoInicioProcesso { get; set; }
    public string? HeartbeatEm { get; set; }
    
    public string? PortaoExpiraEm { get; set; }
    public string CriadaEm { get; set; } = string.Empty;
    public string AtualizadaEm { get; set; } = string.Empty;
    public string? EncerradaEm { get; set; }
    public string? MotivoFalha { get; set; }
}
