using FolderOrganIAzer.Dominio;
using Xunit;

namespace FolderOrganIAzer.Testes.Unidade;

public class MaquinaDeEstadosTestes
{
    [Fact]
    public void ValidarTransicao_EstadoFinal_RecusaQualquerTransicao()
    {
        var resultado = MaquinaDeEstados.ValidarTransicao(EstadoExecucao.CONCLUIDA, EstadoExecucao.INVENTARIANDO);
        
        Assert.False(resultado.Sucesso);
        Assert.Contains("estado final", resultado.Erro);
    }

    [Fact]
    public void ValidarTransicao_PausarDeEstadoNaoFinal_Permite()
    {
        var resultado = MaquinaDeEstados.ValidarTransicao(EstadoExecucao.INVENTARIANDO, EstadoExecucao.PAUSADA);
        
        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public void ValidarTransicao_TransicaoValidaDoDiagrama_Permite()
    {
        var resultado = MaquinaDeEstados.ValidarTransicao(EstadoExecucao.CRIADA, EstadoExecucao.VALIDANDO);
        
        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public void ValidarTransicao_TransicaoInvalidaForaDoDiagrama_Recusa()
    {
        var resultado = MaquinaDeEstados.ValidarTransicao(EstadoExecucao.VALIDANDO, EstadoExecucao.CLASSIFICANDO);
        
        Assert.False(resultado.Sucesso);
        Assert.Contains("Transição inválida", resultado.Erro);
    }
}
