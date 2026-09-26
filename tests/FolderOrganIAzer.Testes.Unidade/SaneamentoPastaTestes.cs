using Xunit;
using FolderOrganIAzer.Dominio;

namespace FolderOrganIAzer.Testes.Unidade;

public class SaneamentoPastaTestes
{
    [Fact]
    public void NormalizaUnicode()
    {
        string decomposto = "c\u0327\u00e3o";
        string sanado = SaneamentoPasta.SanearNome(decomposto, false);
        Assert.Equal("ção", sanado);
        Assert.Equal(3, sanado.Length);
    }

    [Fact]
    public void RemoveAcentosSePedido()
    {
        string sanado = SaneamentoPasta.SanearNome("Atenção, maçã!", true);
        Assert.Equal("Atencao, maca!", sanado);
    }

    [Fact]
    public void NaoRemoveAcentosPorPadrao()
    {
        string sanado = SaneamentoPasta.SanearNome("Atenção, maçã!", false);
        Assert.Equal("Atenção, maçã!", sanado);
    }

    [Theory]
    [InlineData("Pasta<Nova>", "Pasta Nova")]
    [InlineData("Faturas: 2026", "Faturas 2026")]
    [InlineData("A/B\\C", "A B C")]
    [InlineData("Um|Dois?Tres*Quatro", "Um Dois Tres Quatro")]
    [InlineData("Oi\"\tMundo", "Oi Mundo")]
    public void SubstituiCaracteresProibidosPorEspaco(string entrada, string esperado)
    {
        Assert.Equal(esperado, SaneamentoPasta.SanearNome(entrada, false));
    }

    [Fact]
    public void ReduzMultiplosEspacos()
    {
        Assert.Equal("A B", SaneamentoPasta.SanearNome("A   B", false));
    }

    [Fact]
    public void RemovePontoEEspacoDoFinal()
    {
        Assert.Equal("Fim", SaneamentoPasta.SanearNome("Fim. . ", false));
    }

    [Theory]
    [InlineData("CON", "CON _")]
    [InlineData("prn", "prn _")]
    [InlineData("AUX.txt", "AUX.txt _")]
    [InlineData("COM1", "COM1 _")]
    [InlineData("LPT9.pdf", "LPT9.pdf _")]
    public void AdicionaSufixoEmNomesReservados(string entrada, string esperado)
    {
        Assert.Equal(esperado, SaneamentoPasta.SanearNome(entrada, false));
    }

    [Fact]
    public void TruncaEm60Caracteres()
    {
        string longo = new string('A', 65);
        string sanado = SaneamentoPasta.SanearNome(longo, false);
        Assert.Equal(60, sanado.Length);
        Assert.Equal(new string('A', 60), sanado);
    }

    [Fact]
    public void TruncaPontoEEspacoSeEstiverAposTruncar()
    {
        string longo = new string('A', 59) + ". B";
        string sanado = SaneamentoPasta.SanearNome(longo, false);
        Assert.Equal(new string('A', 59), sanado);
    }

    [Fact]
    public void ConverteVazioParaUnderline()
    {
        Assert.Equal("_", SaneamentoPasta.SanearNome("   ", false));
        Assert.Equal("_", SaneamentoPasta.SanearNome("...", false));
    }
}
