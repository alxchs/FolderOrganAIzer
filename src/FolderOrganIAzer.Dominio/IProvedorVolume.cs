namespace FolderOrganIAzer.Dominio;

public interface IProvedorVolume
{
    (string TipoUnidade, string SistemaArquivos) ObterInfoVolume(string caminho);
    long ObterEspacoLivreAppData();
}
