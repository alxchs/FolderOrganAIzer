namespace FolderOrganIAzer.Dominio;

public static class MaquinaDeEstados
{
    public static ResultadoTransicao ValidarTransicao(EstadoExecucao atual, EstadoExecucao proximo)
    {
        if (EhEstadoFinal(atual))
        {
            return ResultadoTransicao.Falha($"Não é possível transicionar a partir de um estado final ({atual}).");
        }

        if (proximo == EstadoExecucao.PAUSADA || proximo == EstadoExecucao.CANCELADA || proximo == EstadoExecucao.FALHOU)
        {
            return ResultadoTransicao.Ok();
        }

        bool valido = atual switch
        {
            EstadoExecucao.CRIADA => proximo == EstadoExecucao.VALIDANDO,
            EstadoExecucao.VALIDANDO => proximo is EstadoExecucao.AGUARDANDO_CONFIRMACAO or EstadoExecucao.INVENTARIANDO,
            EstadoExecucao.AGUARDANDO_CONFIRMACAO => proximo == EstadoExecucao.INVENTARIANDO,
            EstadoExecucao.INVENTARIANDO => proximo == EstadoExecucao.EXTRAINDO,
            EstadoExecucao.EXTRAINDO => proximo is EstadoExecucao.CLASSIFICANDO or EstadoExecucao.AGUARDANDO_CUSTO,
            EstadoExecucao.AGUARDANDO_CUSTO => proximo == EstadoExecucao.CLASSIFICANDO,
            EstadoExecucao.CLASSIFICANDO => proximo is EstadoExecucao.REFINANDO or EstadoExecucao.PLANEJANDO,
            EstadoExecucao.REFINANDO => proximo is EstadoExecucao.AGUARDANDO_REVISAO or EstadoExecucao.PLANEJANDO,
            EstadoExecucao.AGUARDANDO_REVISAO => proximo == EstadoExecucao.PLANEJANDO,
            EstadoExecucao.PLANEJANDO => proximo == EstadoExecucao.CRIANDO_PASTAS,
            EstadoExecucao.CRIANDO_PASTAS => proximo == EstadoExecucao.MOVENDO,
            EstadoExecucao.MOVENDO => proximo == EstadoExecucao.FINALIZANDO,
            EstadoExecucao.FINALIZANDO => proximo == EstadoExecucao.CONCLUIDA,
            EstadoExecucao.PAUSADA => false, 
            _ => false
        };

        if (valido)
            return ResultadoTransicao.Ok();

        return ResultadoTransicao.Falha($"Transição inválida de {atual} para {proximo}.");
    }

    private static bool EhEstadoFinal(EstadoExecucao estado) =>
        estado is EstadoExecucao.CONCLUIDA or EstadoExecucao.CANCELADA or EstadoExecucao.FALHOU;
}
