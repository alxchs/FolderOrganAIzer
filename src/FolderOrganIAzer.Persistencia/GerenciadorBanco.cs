using Microsoft.Data.Sqlite;
using System.Reflection;

namespace FolderOrganIAzer.Persistencia;

public class GerenciadorBanco
{
    private readonly string _caminhoBanco;
    private readonly string _caminhoPastaDados;

    public GerenciadorBanco(string caminhoPastaDados)
    {
        _caminhoPastaDados = caminhoPastaDados;
        _caminhoBanco = Path.Combine(caminhoPastaDados, "folderorganiazer.db");
    }

    public string CaminhoBanco => _caminhoBanco;

    public SqliteConnection CriarConexao()
    {
        if (!Directory.Exists(_caminhoPastaDados))
        {
            Directory.CreateDirectory(_caminhoPastaDados);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _caminhoBanco
        };

        var conexao = new SqliteConnection(builder.ToString());
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = FULL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
        ";
        comando.ExecuteNonQuery();

        return conexao;
    }

    public void GarantirEsquema()
    {
        using var conexao = CriarConexao();
        
        using var comandoVersao = conexao.CreateCommand();
        comandoVersao.CommandText = "PRAGMA user_version;";
        var versaoAtual = (long)(comandoVersao.ExecuteScalar() ?? 0L);

        var assembly = typeof(GerenciadorBanco).Assembly;
        var scripts = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Migracoes.") && n.EndsWith(".sql"))
            .OrderBy(n => n)
            .ToList();

        if (scripts.Count == 0) return;

        long versaoAlvo = scripts.Count;
        if (versaoAtual >= versaoAlvo) return;

        FazerBackupAntesMigracao(conexao);

        for (int i = (int)versaoAtual; i < scripts.Count; i++)
        {
            var nomeRecurso = scripts[i];
            using var stream = assembly.GetManifestResourceStream(nomeRecurso);
            if (stream == null) continue;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            using var transacao = conexao.BeginTransaction();
            try
            {
                using var comandoMigracao = conexao.CreateCommand();
                comandoMigracao.Transaction = transacao;
                comandoMigracao.CommandText = sql;
                comandoMigracao.ExecuteNonQuery();

                long novaVersao = i + 1;
                using var comandoAtualizaVersao = conexao.CreateCommand();
                comandoAtualizaVersao.Transaction = transacao;
                comandoAtualizaVersao.CommandText = $"PRAGMA user_version = {novaVersao};";
                comandoAtualizaVersao.ExecuteNonQuery();

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }
    }

    private void FazerBackupAntesMigracao(SqliteConnection conexaoOrigem)
    {
        if (!File.Exists(_caminhoBanco))
            return;

        var info = new FileInfo(_caminhoBanco);
        if (info.Length == 0) return;

        string nomeBackup = $"folderorganiazer_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        string caminhoBackup = Path.Combine(_caminhoPastaDados, nomeBackup);

        using var comando = conexaoOrigem.CreateCommand();
        comando.CommandText = $"VACUUM INTO '{caminhoBackup.Replace("'", "''")}';";
        comando.ExecuteNonQuery();
    }
}
