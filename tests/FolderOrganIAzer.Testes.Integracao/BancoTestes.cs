using Xunit;
using FolderOrganIAzer.Persistencia;
using System.IO;
using Microsoft.Data.Sqlite;
using System;

namespace FolderOrganIAzer.Testes.Integracao;

public class BancoTestes : IDisposable
{
    private readonly string _tempDir;
    private readonly GerenciadorBanco _gerenciador;

    public BancoTestes()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_DbTestes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _gerenciador = new GerenciadorBanco(_tempDir);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public void CriarConexao_AplicaPragmas()
    {
        using var conexao = _gerenciador.CriarConexao();

        var pragmas = new[] { "journal_mode", "synchronous", "foreign_keys", "busy_timeout" };
        var resultados = new System.Collections.Generic.Dictionary<string, string>();

        foreach (var p in pragmas)
        {
            using var cmd = conexao.CreateCommand();
            cmd.CommandText = $"PRAGMA {p};";
            resultados[p] = cmd.ExecuteScalar()?.ToString() ?? "";
        }

        Assert.Equal("wal", resultados["journal_mode"].ToLower());
        Assert.Equal("2", resultados["synchronous"]);
        Assert.Equal("1", resultados["foreign_keys"]);
        Assert.Equal("5000", resultados["busy_timeout"]);
    }

    [Fact]
    public void GarantirEsquema_EsquemaCriadoComVersao1()
    {
        _gerenciador.GarantirEsquema();

        using var conexao = _gerenciador.CriarConexao();
        
        using var cmdVersao = conexao.CreateCommand();
        cmdVersao.CommandText = "PRAGMA user_version;";
        var v = (long)cmdVersao.ExecuteScalar()!;
        Assert.Equal(1, v);

        using var cmdTabela = conexao.CreateCommand();
        cmdTabela.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='historico';";
        var tabelas = (long)cmdTabela.ExecuteScalar()!;
        Assert.Equal(1, tabelas);
    }

    [Fact]
    public void GarantirEsquema_SegundaExecucaoSemEfeitoESemCopia()
    {
        _gerenciador.GarantirEsquema();
        
        var backupsIniciais = Directory.GetFiles(_tempDir, "folderorganiazer_backup_*.db");
        int quantidadeIniciais = backupsIniciais.Length;

        _gerenciador.GarantirEsquema();

        var backupsFinais = Directory.GetFiles(_tempDir, "folderorganiazer_backup_*.db");
        Assert.Equal(quantidadeIniciais, backupsFinais.Length);
    }

    [Fact]
    public void GarantirEsquema_BancoMenorCriaVacuumInto()
    {
        using (var conexao = _gerenciador.CriarConexao())
        {
            using var cmd = conexao.CreateCommand();
            cmd.CommandText = "CREATE TABLE dummy (id INT); INSERT INTO dummy VALUES(1);";
            cmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();

        _gerenciador.GarantirEsquema();

        var backups = Directory.GetFiles(_tempDir, "folderorganiazer_backup_*.db");
        Assert.Single(backups);
    }

    [Fact]
    public void Historico_GatilhosRecusamUpdateEDelete()
    {
        _gerenciador.GarantirEsquema();

        using var conexao = _gerenciador.CriarConexao();
        
        using var cmdSetup = conexao.CreateCommand();
        cmdSetup.CommandText = @"
            INSERT INTO raiz (id, caminho, volume_serial, file_id, sistema_arquivos, criada_em) VALUES (1, 'C', 1, x'01', 'NTFS', '2026-01-01');
            INSERT INTO execucao (id, raiz_id, estado, config_json, versao_app, versao_taxonomia, versao_prompt, criada_em, atualizada_em) 
                VALUES ('exec1', 1, 'CRIADA', '{}', '1', 1, 1, '2026', '2026');
            INSERT INTO historico (execucao_id, ocorrido_em, tipo, detalhes_json, hash_encadeado) 
                VALUES ('exec1', '2026-01-01', 'TIPO', '{}', x'00');
        ";
        cmdSetup.ExecuteNonQuery();

        using var cmdUpdate = conexao.CreateCommand();
        cmdUpdate.CommandText = "UPDATE historico SET tipo = 'ALTERADO';";
        var exUpdate = Assert.Throws<SqliteException>(() => cmdUpdate.ExecuteNonQuery());
        Assert.Contains("historico e somente insercao", exUpdate.Message);

        using var cmdDelete = conexao.CreateCommand();
        cmdDelete.CommandText = "DELETE FROM historico;";
        var exDelete = Assert.Throws<SqliteException>(() => cmdDelete.ExecuteNonQuery());
        Assert.Contains("historico e somente insercao", exDelete.Message);
    }
}
