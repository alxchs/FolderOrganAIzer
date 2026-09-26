using System;
using System.Diagnostics;
using System.Globalization;
using FolderOrganIAzer.Dominio;
using Xunit;

namespace FolderOrganIAzer.Testes.Unidade;

public class GerenciadorTravaTestes
{
    private readonly GerenciadorTrava _trava = new();

    [Fact]
    public void EhTravaOrfa_SemDono_RetornaFalso()
    {
        var exec = new Execucao { DonoPid = null };
        var orfa = _trava.EhTravaOrfa(exec);
        Assert.False(orfa);
    }

    [Fact]
    public void EhTravaOrfa_BatimentoMuitoAntigo_RetornaVerdadeiro()
    {
        var processoAtual = Process.GetCurrentProcess();
        var exec = new Execucao 
        { 
            DonoPid = processoAtual.Id,
            DonoInicioProcesso = FormatadorData.Formatar(processoAtual.StartTime.ToUniversalTime()),
            HeartbeatEm = FormatadorData.Formatar(DateTime.UtcNow.AddSeconds(-65))
        };

        var orfa = _trava.EhTravaOrfa(exec);
        Assert.True(orfa);
    }

    [Fact]
    public void EhTravaOrfa_PidNaoExiste_RetornaVerdadeiro()
    {
        
        var exec = new Execucao 
        { 
            DonoPid = 999999, 
            DonoInicioProcesso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        };

        var orfa = _trava.EhTravaOrfa(exec);
        Assert.True(orfa);
    }

    [Fact]
    public void EhTravaOrfa_PidExisteMasOutraHoraDeInicio_RetornaVerdadeiro()
    {
        var processoAtual = Process.GetCurrentProcess();
        var exec = new Execucao 
        { 
            DonoPid = processoAtual.Id,
            DonoInicioProcesso = DateTime.UtcNow.AddMinutes(-10).ToString("o", CultureInfo.InvariantCulture) 
        };

        var orfa = _trava.EhTravaOrfa(exec);
        Assert.True(orfa);
    }

    [Fact]
    public void EhTravaOrfa_ProcessoValidoEBatimentoRecente_RetornaFalso()
    {
        var processoAtual = Process.GetCurrentProcess();
        var exec = new Execucao 
        { 
            DonoPid = processoAtual.Id,
            DonoInicioProcesso = FormatadorData.Formatar(processoAtual.StartTime.ToUniversalTime()),
            HeartbeatEm = FormatadorData.Formatar(DateTime.UtcNow.AddSeconds(-10))
        };

        var orfa = _trava.EhTravaOrfa(exec);
        Assert.False(orfa);
    }

    [Fact]
    public void AssumirEHeartbeatEZerar_TestamTrava()
    {
        var exec = new Execucao();
        var trava = new GerenciadorTrava();

        trava.Assumir(exec);
        Assert.NotNull(exec.DonoPid);
        Assert.NotNull(exec.DonoInicioProcesso);
        Assert.NotNull(exec.HeartbeatEm);

        var antigoHeartbeat = exec.HeartbeatEm;
        System.Threading.Thread.Sleep(10);
        trava.PulsarHeartbeat(exec);
        Assert.NotEqual(antigoHeartbeat, exec.HeartbeatEm);

        trava.Zerar(exec);
        Assert.Null(exec.DonoPid);
        Assert.Null(exec.DonoInicioProcesso);
        Assert.Null(exec.HeartbeatEm);
    }

}
