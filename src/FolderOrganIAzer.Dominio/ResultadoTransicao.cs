namespace FolderOrganIAzer.Dominio;

public class ResultadoTransicao
{
    public bool Sucesso { get; }
    public string? Erro { get; }

    private ResultadoTransicao(bool sucesso, string? erro)
    {
        Sucesso = sucesso;
        Erro = erro;
    }

    public static ResultadoTransicao Ok() => new ResultadoTransicao(true, null);
    public static ResultadoTransicao Falha(string erro) => new ResultadoTransicao(false, erro);
}
