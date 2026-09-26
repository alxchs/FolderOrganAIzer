namespace FolderOrganIAzer.Dominio;

public enum SeveridadeAchado
{
    AVISO,
    CONFIRMACAO,
    BLOQUEIO
}

public class AchadoValidacao
{
    public string Codigo { get; set; } = string.Empty;
    public SeveridadeAchado Severidade { get; set; }
    public string? Mensagem { get; set; }
    public object? Detalhes { get; set; }
}
