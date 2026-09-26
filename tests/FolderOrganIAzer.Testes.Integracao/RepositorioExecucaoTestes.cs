using System;
using FolderOrganIAzer.Dominio;
using FolderOrganIAzer.Persistencia;
using Xunit;
using Dapper;
using System.Linq;

namespace FolderOrganIAzer.Testes.Integracao;

public class RepositorioExecucaoTestes : IDisposable
{
    private readonly string _tempDir;
    private readonly GerenciadorBanco _banco;
    private readonly RepositorioExecucao _repo;

    public RepositorioExecucaoTestes()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_RepoTestes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _banco = new GerenciadorBanco(_tempDir);
        _banco.GarantirEsquema();
        _repo = new RepositorioExecucao(_banco);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public void GravarTransicao_CadeiaDeHashValidaAposVariasTransicoes()
    {
        var raiz = new Raiz { Caminho = @"C:\teste", VolumeSerial = 123, FileId = new byte[16], SistemaArquivos = "NTFS", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        var raizId = _repo.RegistrarRaiz(raiz);

        var exec = new Execucao { Id = Guid.NewGuid().ToString(), RaizId = raizId, Estado = EstadoExecucao.CRIADA, CriadaEm = FormatadorData.Formatar(DateTime.UtcNow), AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        _repo.CriarExecucao(exec);

        _repo.GravarTransicao(exec, EstadoExecucao.VALIDANDO.ToString(), "{}");
        _repo.GravarTransicao(exec, EstadoExecucao.INVENTARIANDO.ToString(), "{}");
        _repo.GravarTransicao(exec, EstadoExecucao.EXTRAINDO.ToString(), "{}");

        using var conexao = _banco.CriarConexao();
        var historico = conexao.Query<HistoricoEvento>("SELECT * FROM historico ORDER BY id ASC").ToList();

        Assert.Equal(3, historico.Count);
        Assert.NotEmpty(historico[0].HashEncadeado);
        Assert.NotEmpty(historico[1].HashEncadeado);
        Assert.NotEmpty(historico[2].HashEncadeado);
        
        
        Assert.NotEqual(historico[0].HashEncadeado, historico[1].HashEncadeado);
        Assert.NotEqual(historico[1].HashEncadeado, historico[2].HashEncadeado);
    }

    [Fact]
    public void RegistrarRaiz_CriaERetornaId()
    {
        var raiz = new Raiz { Caminho = @"C:	este2", VolumeSerial = 124, FileId = new byte[16], SistemaArquivos = "NTFS", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        var raizId = _repo.RegistrarRaiz(raiz);
        Assert.True(raizId > 0);
        var raizLida = _repo.ObterRaiz(raizId);
        Assert.NotNull(raizLida);
        Assert.Equal(@"C:	este2", raizLida.Caminho);
    }

    [Fact]
    public void CriarExecucao_GravaConfiguracaoCongelada()
    {
        var raiz = new Raiz { Caminho = @"C:	este3", VolumeSerial = 125, FileId = new byte[16], SistemaArquivos = "NTFS", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        var raizId = _repo.RegistrarRaiz(raiz);

        var exec = new Execucao { Id = Guid.NewGuid().ToString(), RaizId = raizId, Estado = EstadoExecucao.CRIADA, ConfigJson = "{\"cong\": true}", CriadaEm = FormatadorData.Formatar(DateTime.UtcNow), AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow) };
        _repo.CriarExecucao(exec);

        using var conexao = _banco.CriarConexao();
        var execLida = conexao.Query<Execucao>("SELECT * FROM execucao WHERE id = @Id", new { Id = exec.Id }).FirstOrDefault();
        Assert.NotNull(execLida);
        Assert.Equal("{\"cong\": true}", execLida.ConfigJson);
    }


}
