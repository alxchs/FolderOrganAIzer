using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FolderOrganIAzer.Dominio;
using FolderOrganIAzer.Motor;
using Dapper;
using FolderOrganIAzer.Persistencia;
using Xunit;

namespace FolderOrganIAzer.Testes.Integracao;

public class GerenciadorExecucaoTestes : IDisposable
{
    private readonly string _tempDbDir;
    private readonly GerenciadorBanco _banco;
    private readonly RepositorioExecucao _repo;
    private readonly FalsoProvedorIA _provedorIA;
    private readonly FalsoProvedorVolume _provedorVolume;
    private readonly ValidadorRaiz _validador;
    private readonly GerenciadorTrava _trava;
    private readonly GerenciadorExecucao _gerenciador;

    public GerenciadorExecucaoTestes()
    {
        _tempDbDir = Path.Combine(AppContext.BaseDirectory, "FolderOrganIAzer_GerenciadorExecucao_Db", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDbDir);
        _banco = new GerenciadorBanco(_tempDbDir);
        _banco.GarantirEsquema();
        _repo = new RepositorioExecucao(_banco);
        _provedorIA = new FalsoProvedorIA();
        _provedorVolume = new FalsoProvedorVolume();
        _validador = new ValidadorRaiz(_repo, _provedorIA, _provedorVolume);
        _trava = new GerenciadorTrava();
        _gerenciador = new GerenciadorExecucao(_repo, _validador, _trava);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDbDir, true); } catch { }
    }

    [Fact]
    public async Task FluxoCompleto_AchadoBloqueio_LevaAFalhou()
    {
        var pasta = Path.Combine(_tempDbDir, "raiz_invalida");
        Directory.CreateDirectory(pasta);
        _provedorVolume.TipoUnidade = "CD-ROM"; 

        await _gerenciador.IniciarExecucaoAsync(pasta, false);

        var exec = _repo.ListarExecucoesAtivas().FirstOrDefault() ?? _banco.CriarConexao().Query<Execucao>("SELECT * FROM execucao").FirstOrDefault();
        Assert.NotNull(exec);
        Assert.Equal(EstadoExecucao.FALHOU, exec!.Estado);
        Assert.Equal("VOLUME_NAO_SUPORTADO", exec!.MotivoFalha);
        Assert.Null(exec!.DonoPid); 
    }

    [Fact]
    public async Task FluxoCompleto_AchadoConfirmacao_LevaAAguardandoConfirmacao_RespostaSim_LevaAInventariando()
    {
        var pasta = Path.Combine(_tempDbDir, "raiz_confirma");
        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, ".organizador"), "{\"Formato\": \"organizador-de-pastas/marcador\", \"Nivel\": 1, \"CriadaEm\": \"2026-09-24T14:05:31.207Z\"}");

        await _gerenciador.IniciarExecucaoAsync(pasta, false);

        var exec = _repo.ListarExecucoesAtivas().FirstOrDefault();
        Assert.NotNull(exec);
        Assert.Equal(EstadoExecucao.AGUARDANDO_CONFIRMACAO, exec!.Estado);

        _gerenciador.ResponderConfirmacao(exec, true);

        exec = _repo.ListarExecucoesAtivas().FirstOrDefault();
        Assert.Equal(EstadoExecucao.INVENTARIANDO, exec!.Estado);
        Assert.NotNull(exec!.DonoPid); 
    }

    [Fact]
    public async Task FluxoCompleto_AchadoConfirmacao_LevaAAguardandoConfirmacao_RespostaNao_LevaACancelada()
    {
        var pasta = Path.Combine(_tempDbDir, "raiz_cancela");
        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, ".organizador"), "{\"Formato\": \"organizador-de-pastas/marcador\", \"Nivel\": 1, \"CriadaEm\": \"2026-09-24T14:05:31.207Z\"}");

        await _gerenciador.IniciarExecucaoAsync(pasta, false);

        var exec = _repo.ListarExecucoesAtivas().FirstOrDefault();
        Assert.NotNull(exec);
        Assert.Equal(EstadoExecucao.AGUARDANDO_CONFIRMACAO, exec!.Estado);

        _gerenciador.ResponderConfirmacao(exec, false);

        exec = _banco.CriarConexao().Query<Execucao>("SELECT * FROM execucao").FirstOrDefault();
        Assert.Equal(EstadoExecucao.CANCELADA, exec!.Estado);
        Assert.Null(exec!.DonoPid); 
    }

    [Fact]
    public async Task IniciarExecucao_GravaHeartbeatACada5Segundos()
    {
        var pasta = Path.Combine(_tempDbDir, "raiz_heartbeat");
        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, ".organizador"), "{\"Formato\": \"organizador-de-pastas/marcador\", \"Nivel\": 1, \"CriadaEm\": \"2026-09-24T14:05:31.207Z\"}");

        await _gerenciador.IniciarExecucaoAsync(pasta, false);

        var exec = _banco.CriarConexao().Query<Execucao>("SELECT * FROM execucao").First();
        var primeiroHeartbeat = exec!.HeartbeatEm;

        await Task.Delay(5500);

        exec = _banco.CriarConexao().Query<Execucao>("SELECT * FROM execucao").First();
        var segundoHeartbeat = exec!.HeartbeatEm;

        Assert.NotEqual(primeiroHeartbeat, segundoHeartbeat);
        
        _gerenciador.Pausar(exec);
    }

}
