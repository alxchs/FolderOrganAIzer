using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FolderOrganIAzer.Dominio;
using FolderOrganIAzer.Motor;
using FolderOrganIAzer.Persistencia;
using Xunit;
using Dapper;

namespace FolderOrganIAzer.Testes.Integracao;

public class FalsoProvedorIA : IProvedorIA
{
    public bool Disponivel { get; set; } = true;
    public Task<bool> TestarDisponibilidadeAsync() => Task.FromResult(Disponivel);
}

public class FalsoProvedorVolume : IProvedorVolume
{
    public string TipoUnidade { get; set; } = "Fixo";
    public string SistemaArquivos { get; set; } = "NTFS";
    public long EspacoLivreAppData { get; set; } = 1000L * 1024 * 1024;

    public (string TipoUnidade, string SistemaArquivos) ObterInfoVolume(string caminho) => (TipoUnidade, SistemaArquivos);
    public long ObterEspacoLivreAppData() => EspacoLivreAppData;
}

public class ValidadorRaizTestes : IDisposable
{
    private readonly string _tempDbDir;
    private readonly GerenciadorBanco _banco;
    private readonly RepositorioExecucao _repo;
    private readonly FalsoProvedorIA _provedorIA;
    private readonly FalsoProvedorVolume _provedorVolume;
    private readonly ValidadorRaiz _validador;
    private readonly List<string> _pastasParaLimpar = new();

    public ValidadorRaizTestes()
    {
        _tempDbDir = Path.Combine(AppContext.BaseDirectory, "FolderOrganIAzer_ValidadorTestes_Db_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDbDir);
        _pastasParaLimpar.Add(_tempDbDir);
        _banco = new GerenciadorBanco(_tempDbDir);
        _banco.GarantirEsquema();
        _repo = new RepositorioExecucao(_banco);
        _provedorIA = new FalsoProvedorIA();
        _provedorVolume = new FalsoProvedorVolume();
        _validador = new ValidadorRaiz(_repo, _provedorIA, _provedorVolume);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var pasta in _pastasParaLimpar)
        {
            try { if (Directory.Exists(pasta)) Directory.Delete(pasta, true); } catch { }
        }
    }

    private string CriarPastaTemp()
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, "teste_raiz_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(pasta);
        _pastasParaLimpar.Add(pasta);
        return pasta;
    }

    [Fact]
    public async Task ValidarAsync_RaizNaoExiste_RetornaBloqueio()
    {
        var achados = await _validador.ValidarAsync(Path.Combine(AppContext.BaseDirectory, "nao_existe"));
        Assert.Contains(achados, a => a.Codigo == "RAIZ_INEXISTENTE" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }

    [Fact]
    public async Task ValidarAsync_LocalProtegido_RetornaBloqueio()
    {
        var achados = await _validador.ValidarAsync(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        Assert.Contains(achados, a => a.Codigo == "LOCAL_PROTEGIDO" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }

    [Fact]
    public async Task ValidarAsync_AcessoNegadoAoTestarEscrita_RetornaBloqueio()
    {
        var pasta = CriarPastaTemp();
        var psi = new System.Diagnostics.ProcessStartInfo("icacls", $"\"{pasta}\" /deny *S-1-1-0:(W)");
        psi.CreateNoWindow = true; psi.UseShellExecute = false; System.Diagnostics.Process.Start(psi)!.WaitForExit();

        try
        {
            var achados = await _validador.ValidarAsync(pasta);
            Assert.Contains(achados, a => a.Codigo == "SEM_PERMISSAO_ESCRITA" && a.Severidade == SeveridadeAchado.BLOQUEIO);
        }
        finally
        {
            var psi2 = new System.Diagnostics.ProcessStartInfo("icacls", $"\"{pasta}\" /remove:d *S-1-1-0");
            psi2.CreateNoWindow = true; psi2.UseShellExecute = false; System.Diagnostics.Process.Start(psi2)!.WaitForExit();
        }
    }

    [Fact]
    public async Task ValidarAsync_PastaGeradaPorMarcador_RetornaConfirmacao()
    {
        var pasta = CriarPastaTemp();
        File.WriteAllText(Path.Combine(pasta, ".organizador"), "{\"Formato\": \"organizador-de-pastas/marcador\", \"Nivel\": 1, \"CriadaEm\": \"2026-09-24T14:05:31.207Z\"}");
        var achados = await _validador.ValidarAsync(pasta);
        Assert.Contains(achados, a => a.Codigo == "RAIZ_E_PASTA_GERADA" && a.Severidade == SeveridadeAchado.CONFIRMACAO);
    }

    [Fact]
    public async Task ValidarAsync_PastaGeradaPorRegistro_RetornaConfirmacao()
    {
        var pasta = CriarPastaTemp();
        var raizOutra = new Raiz { Caminho = @"C:\outra", VolumeSerial = 99, FileId = new byte[16], SistemaArquivos = "NTFS", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        var raizId = _repo.RegistrarRaiz(raizOutra);

        using var conexao = _banco.CriarConexao();
        var execId = Guid.NewGuid().ToString();
        conexao.Execute(
            "INSERT INTO execucao (id, raiz_id, estado, config_json, versao_app, versao_taxonomia, versao_prompt, criada_em, atualizada_em) VALUES (@Id, @RaizId, 'CONCLUIDA', '{}', '1.0', 1, 1, @CriadaEm, @AtualizadaEm)",
            new { Id = execId, RaizId = raizId, CriadaEm = FormatadorData.Formatar(DateTime.UtcNow), AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow) });

        var info = FolderOrganIAzer.SistemaArquivos.OperacoesDisco.LerInfo(pasta, ehPasta: true);
        conexao.Execute("INSERT INTO pasta_gerada (marcador_guid, raiz_id, criada_por_execucao, nivel, nome, caminho_relativo, volume_serial, file_id, criada_em) VALUES (@Guid, @RaizId, @ExecId, 1, 'nome', 'caminho', @VolumeSerial, @FileId, @CriadaEm)",
            new { Guid = Guid.NewGuid().ToString(), RaizId = raizId, ExecId = execId, VolumeSerial = (long)info.Identidade.VolumeSerial, FileId = info.Identidade.FileId.ToByteArray(), CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) });

        var achados = await _validador.ValidarAsync(pasta);
        Assert.Contains(achados, a => a.Codigo == "RAIZ_E_PASTA_GERADA" && a.Severidade == SeveridadeAchado.CONFIRMACAO);
    }

    [Fact]
    public async Task ValidarAsync_VolumeNaoSuportado_RetornaBloqueio()
    {
        var pasta = CriarPastaTemp();
        _provedorVolume.TipoUnidade = "CD-ROM";
        var achados = await _validador.ValidarAsync(pasta);
        Assert.Contains(achados, a => a.Codigo == "VOLUME_NAO_SUPORTADO" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }

    [Fact]
    public async Task ValidarAsync_SistemaArquivosNaoSuportado_RetornaBloqueio()
    {
        var pasta = CriarPastaTemp();
        _provedorVolume.SistemaArquivos = "FAT32";
        var achados = await _validador.ValidarAsync(pasta);
        Assert.Contains(achados, a => a.Codigo == "SISTEMA_ARQUIVOS_NAO_SUPORTADO" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }

    [Fact]
    public async Task ValidarAsync_PoucoEspacoDados_RetornaAviso()
    {
        var pasta = CriarPastaTemp();
        _provedorVolume.EspacoLivreAppData = 100L * 1024 * 1024;
        var achados = await _validador.ValidarAsync(pasta);
        Assert.Contains(achados, a => a.Codigo == "POUCO_ESPACO_DADOS" && a.Severidade == SeveridadeAchado.AVISO);
    }

    [Fact]
    public async Task ValidarAsync_CaminhoLongo_RetornaAviso()
    {
        var pasta = CriarPastaTemp();
        var longo = pasta;
        while (longo.Length < 259) longo = Path.Combine(longo, "pasta_longa");
        Directory.CreateDirectory(longo);
        _pastasParaLimpar.Add(longo);
        var achados = await _validador.ValidarAsync(longo);
        Assert.Contains(achados, a => a.Codigo == "CAMINHO_LONGO" && a.Severidade == SeveridadeAchado.AVISO);
    }

    [Fact]
    public async Task ValidarAsync_RaizDeUnidade_RetornaAviso()
    {
        var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c subst Z: \"{AppContext.BaseDirectory.TrimEnd('\\')}\"");
        psi.CreateNoWindow = true; psi.UseShellExecute = false; System.Diagnostics.Process.Start(psi)!.WaitForExit();
        
        try
        {
            var pasta = @"Z:\";
            var achados = await _validador.ValidarAsync(pasta);
            Assert.Contains(achados, a => a.Codigo == "RAIZ_DE_UNIDADE" && a.Severidade == SeveridadeAchado.AVISO);
        }
        finally
        {
            var psi2 = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c subst Z: /D");
            psi2.CreateNoWindow = true; psi2.UseShellExecute = false; System.Diagnostics.Process.Start(psi2)!.WaitForExit();
        }
    }

    [Fact]
    public async Task ValidarAsync_RaizELink_RetornaBloqueio()
    {
        var pasta = CriarPastaTemp();
        var link = Path.Combine(AppContext.BaseDirectory, "organizador-testes-link-" + Guid.NewGuid());
        _pastasParaLimpar.Add(link);
        
        var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{pasta}\"");
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        var proc = System.Diagnostics.Process.Start(psi);
        proc!.WaitForExit();

        var achados = await _validador.ValidarAsync(link);
        Assert.Contains(achados, a => a.Codigo == "RAIZ_E_LINK" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }

    [Fact]
    public async Task ValidarAsync_RaizEmUsoAncestral_RetornaBloqueio()
    {
        var pai = CriarPastaTemp();
        var filha = Path.Combine(pai, "filha");
        Directory.CreateDirectory(filha);

        var raiz = new Raiz { Caminho = pai, VolumeSerial = 1, FileId = new byte[16], SistemaArquivos = "NTFS", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        var raizId = _repo.RegistrarRaiz(raiz);
        var exec = new Execucao { Id = Guid.NewGuid().ToString(), RaizId = raizId, Estado = EstadoExecucao.CRIADA, CriadaEm = FormatadorData.Formatar(DateTime.UtcNow), AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        _repo.CriarExecucao(exec);

        var achados = await _validador.ValidarAsync(filha);
        Assert.Contains(achados, a => a.Codigo == "RAIZ_EM_USO" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }

    [Fact]
    public async Task ValidarAsync_RaizEmUsoDescendente_RetornaBloqueio()
    {
        var pai = CriarPastaTemp();
        var filha = Path.Combine(pai, "filha");
        Directory.CreateDirectory(filha);

        var raiz = new Raiz { Caminho = filha, VolumeSerial = 1, FileId = new byte[16], SistemaArquivos = "NTFS", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        var raizId = _repo.RegistrarRaiz(raiz);
        var exec = new Execucao { Id = Guid.NewGuid().ToString(), RaizId = raizId, Estado = EstadoExecucao.CRIADA, CriadaEm = FormatadorData.Formatar(DateTime.UtcNow), AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        _repo.CriarExecucao(exec);

        var achados = await _validador.ValidarAsync(pai);
        Assert.Contains(achados, a => a.Codigo == "RAIZ_EM_USO" && a.Severidade == SeveridadeAchado.BLOQUEIO);
    }
}
