using System.Collections.Generic;

namespace FolderOrganIAzer.Dominio;

public interface IRepositorioExecucao
{
    long RegistrarRaiz(Raiz raiz);
    void CriarExecucao(Execucao execucao);
    void GravarTransicao(Execucao execucao, string novoEstado, string detalhesJson, string? origem, string? destino);
    IEnumerable<Execucao> ListarExecucoesAtivas();
    Raiz? ObterRaiz(long raizId);
    PastaGerada? ObterPastaGeradaPorIdentidade(long volumeSerial, byte[] fileId);
    void AtualizarTrava(Execucao execucao);
}
