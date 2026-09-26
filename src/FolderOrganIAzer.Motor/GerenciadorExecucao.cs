using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FolderOrganIAzer.Dominio;

namespace FolderOrganIAzer.Motor;

public class GerenciadorExecucao : IDisposable
{
    private readonly IRepositorioExecucao _repositorio;
    private readonly ValidadorRaiz _validador;
    private readonly GerenciadorTrava _trava;
    private Timer? _timer;
    private Execucao? _execucaoAtiva;

    public GerenciadorExecucao(IRepositorioExecucao repositorio, ValidadorRaiz validador, GerenciadorTrava trava)
    {
        _repositorio = repositorio;
        _validador = validador;
        _trava = trava;
    }

    public async Task IniciarExecucaoAsync(string caminhoRaiz, bool usarIa)
    {
        var raiz = new Raiz
        {
            Caminho = caminhoRaiz,
            VolumeSerial = 1,
            FileId = new byte[16],
            SistemaArquivos = "NTFS",
            CriadaEm = FormatadorData.Formatar(DateTime.UtcNow)
        };
        var raizId = _repositorio.RegistrarRaiz(raiz);

        var execucao = new Execucao
        {
            Id = Guid.NewGuid().ToString(),
            RaizId = raizId,
            Estado = EstadoExecucao.CRIADA,
            CriadaEm = FormatadorData.Formatar(DateTime.UtcNow),
            AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow),
            ConfigJson = "{}",
            VersaoApp = "1.0",
            VersaoTaxonomia = 1,
            VersaoPrompt = 1
        };

        _trava.Assumir(execucao);
        _repositorio.CriarExecucao(execucao);
        _execucaoAtiva = execucao;

        _timer = new Timer(Pulsar, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        _repositorio.GravarTransicao(execucao, EstadoExecucao.VALIDANDO.ToString(), "{}", EstadoExecucao.CRIADA.ToString(), EstadoExecucao.VALIDANDO.ToString());
        _repositorio.AtualizarTrava(execucao);

        var achados = await _validador.ValidarAsync(caminhoRaiz, !usarIa, execucao.Id);

        var bloqueio = achados.FirstOrDefault(a => a.Severidade == SeveridadeAchado.BLOQUEIO);
        if (bloqueio != null)
        {
            execucao.MotivoFalha = bloqueio.Codigo;
            _repositorio.GravarTransicao(execucao, EstadoExecucao.FALHOU.ToString(), "{}", EstadoExecucao.VALIDANDO.ToString(), EstadoExecucao.FALHOU.ToString());
            _trava.Zerar(execucao);
            _repositorio.AtualizarTrava(execucao);
            PararTimer();
            return;
        }

        var confirmacao = achados.FirstOrDefault(a => a.Severidade == SeveridadeAchado.CONFIRMACAO);
        if (confirmacao != null)
        {
            var detalhesJson = JsonSerializer.Serialize(confirmacao.Detalhes, new JsonSerializerOptions { WriteIndented = false });
            _repositorio.GravarTransicao(execucao, EstadoExecucao.AGUARDANDO_CONFIRMACAO.ToString(), detalhesJson, EstadoExecucao.VALIDANDO.ToString(), EstadoExecucao.AGUARDANDO_CONFIRMACAO.ToString());
            _repositorio.AtualizarTrava(execucao);
            return;
        }

        _repositorio.GravarTransicao(execucao, EstadoExecucao.INVENTARIANDO.ToString(), "{}", EstadoExecucao.VALIDANDO.ToString(), EstadoExecucao.INVENTARIANDO.ToString());
        _repositorio.AtualizarTrava(execucao);
    }

    private void Pulsar(object? state)
    {
        if (_execucaoAtiva != null)
        {
            try
            {
                _trava.PulsarHeartbeat(_execucaoAtiva);
                _repositorio.AtualizarTrava(_execucaoAtiva);
            }
            catch
            {
            }
        }
    }

    private void PararTimer()
    {
        _timer?.Dispose();
        _timer = null;
        _execucaoAtiva = null;
    }

    public void ResponderConfirmacao(Execucao execucao, bool confirmado)
    {
        var origem = execucao.Estado.ToString();
        var destino = confirmado ? EstadoExecucao.INVENTARIANDO.ToString() : EstadoExecucao.CANCELADA.ToString();

        _repositorio.GravarTransicao(execucao, destino, "{}", origem, destino);

        if (!confirmado)
        {
            _trava.Zerar(execucao);
            PararTimer();
        }
        _repositorio.AtualizarTrava(execucao);
    }

    public void Pausar(Execucao execucao)
    {
        var origem = execucao.Estado.ToString();
        _repositorio.GravarTransicao(execucao, EstadoExecucao.PAUSADA.ToString(), "{}", origem, EstadoExecucao.PAUSADA.ToString());
        _trava.Zerar(execucao);
        _repositorio.AtualizarTrava(execucao);
        PararTimer();
    }

    public void Dispose()
    {
        PararTimer();
    }
}
