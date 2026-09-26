using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using FolderOrganIAzer.Dominio;
using Microsoft.Data.Sqlite;

namespace FolderOrganIAzer.Persistencia;

public class RepositorioExecucao : IRepositorioExecucao
{
    private readonly GerenciadorBanco _banco;

    public RepositorioExecucao(GerenciadorBanco banco)
    {
        _banco = banco;
    }

    public long RegistrarRaiz(Raiz raiz)
    {
        using var conexao = _banco.CriarConexao();
        var sql = @"
            INSERT INTO raiz (caminho, volume_serial, file_id, sistema_arquivos, criada_em)
            VALUES (@Caminho, @VolumeSerial, @FileId, @SistemaArquivos, @CriadaEm)
            ON CONFLICT(volume_serial, file_id) DO UPDATE SET 
                caminho = excluded.caminho
            RETURNING id;
        ";
        return conexao.ExecuteScalar<long>(sql, raiz);
    }

    public void CriarExecucao(Execucao execucao)
    {
        using var conexao = _banco.CriarConexao();
        var sql = @"
            INSERT INTO execucao (
                id, raiz_id, estado, estado_antes_pausa, confirmada_pelo_usuario_em, config_json, 
                versao_app, versao_taxonomia, versao_prompt, dono_pid, dono_inicio_processo, 
                heartbeat_em, portao_expira_em, criada_em, atualizada_em, encerrada_em, motivo_falha
            ) VALUES (
                @Id, @RaizId, @Estado, @EstadoAntesPausa, @ConfirmadaPeloUsuarioEm, @ConfigJson, 
                @VersaoApp, @VersaoTaxonomia, @VersaoPrompt, @DonoPid, @DonoInicioProcesso, 
                @HeartbeatEm, @PortaoExpiraEm, @CriadaEm, @AtualizadaEm, @EncerradaEm, @MotivoFalha
            );
        ";
        
        var param = new
        {
            execucao.Id,
            execucao.RaizId,
            Estado = execucao.Estado.ToString(),
            EstadoAntesPausa = execucao.EstadoAntesPausa?.ToString(),
            execucao.ConfirmadaPeloUsuarioEm,
            execucao.ConfigJson,
            execucao.VersaoApp,
            execucao.VersaoTaxonomia,
            execucao.VersaoPrompt,
            execucao.DonoPid,
            execucao.DonoInicioProcesso,
            execucao.HeartbeatEm,
            execucao.PortaoExpiraEm,
            execucao.CriadaEm,
            execucao.AtualizadaEm,
            execucao.EncerradaEm,
            execucao.MotivoFalha
        };
        
        conexao.Execute(sql, param);
    }

    public void GravarTransicao(Execucao execucao, string novoEstadoStr, string detalhesJson, string? origem = null, string? destino = null)
    {
        if (!Enum.TryParse<EstadoExecucao>(novoEstadoStr, out var novoEstado))
            throw new ArgumentException("Estado inválido", nameof(novoEstadoStr));

        var resultado = MaquinaDeEstados.ValidarTransicao(execucao.Estado, novoEstado);
        if (!resultado.Sucesso)
            throw new InvalidOperationException($"Transição inválida: {resultado.Erro}");

        using var conexao = _banco.CriarConexao();
        using var transacao = conexao.BeginTransaction();
        try
        {
            execucao.Estado = novoEstado;
            execucao.AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow);
            
            var sqlUpdate = @"
                UPDATE execucao 
                SET estado = @Estado, atualizada_em = @AtualizadaEm, motivo_falha = @MotivoFalha
                WHERE id = @Id;
            ";
            conexao.Execute(sqlUpdate, new { Estado = novoEstadoStr, execucao.AtualizadaEm, execucao.MotivoFalha, execucao.Id }, transacao);

            var sqlUltimoHash = "SELECT hash_encadeado FROM historico WHERE execucao_id = @Id ORDER BY id DESC LIMIT 1;";
            var ultimoHash = conexao.ExecuteScalar<byte[]>(sqlUltimoHash, new { execucao.Id }, transacao) ?? new byte[32];

            var evento = new HistoricoEvento
            {
                ExecucaoId = execucao.Id,
                OcorridoEm = execucao.AtualizadaEm,
                Tipo = "ESTADO_ALTERADO",
                Origem = origem,
                Destino = destino,
                DetalhesJson = detalhesJson
            };

            evento.HashEncadeado = CalcularHash(ultimoHash, evento);

            var sqlInsertHistorico = @"
                INSERT INTO historico (execucao_id, ocorrido_em, tipo, origem, destino, detalhes_json, hash_encadeado)
                VALUES (@ExecucaoId, @OcorridoEm, @Tipo, @Origem, @Destino, @DetalhesJson, @HashEncadeado);
            ";
            conexao.Execute(sqlInsertHistorico, evento, transacao);

            transacao.Commit();
        }
        catch
        {
            transacao.Rollback();
            throw;
        }
    }

    public void AtualizarTrava(Execucao execucao)
    {
        using var conexao = _banco.CriarConexao();
        var sql = @"
            UPDATE execucao 
            SET dono_pid = @DonoPid, 
                dono_inicio_processo = @DonoInicioProcesso, 
                heartbeat_em = @HeartbeatEm,
                atualizada_em = @AtualizadaEm
            WHERE id = @Id;
        ";
        conexao.Execute(sql, new 
        { 
            execucao.DonoPid, 
            execucao.DonoInicioProcesso, 
            execucao.HeartbeatEm, 
            execucao.AtualizadaEm,
            execucao.Id 
        });
    }

    public IEnumerable<Execucao> ListarExecucoesAtivas()
    {
        using var conexao = _banco.CriarConexao();
        return conexao.Query<Execucao>("SELECT * FROM execucao WHERE estado NOT IN ('CONCLUIDA', 'CANCELADA', 'FALHOU');");
    }

    public Raiz? ObterRaiz(long raizId)
    {
        using var conexao = _banco.CriarConexao();
        return conexao.QuerySingleOrDefault<Raiz>("SELECT * FROM raiz WHERE id = @Id;", new { Id = raizId });
    }

    public PastaGerada? ObterPastaGeradaPorIdentidade(long volumeSerial, byte[] fileId)
    {
        using var conexao = _banco.CriarConexao();
        return conexao.QuerySingleOrDefault<PastaGerada>(
            "SELECT * FROM pasta_gerada WHERE volume_serial = @VolumeSerial AND file_id = @FileId;", 
            new { VolumeSerial = volumeSerial, FileId = fileId });
    }

    private byte[] CalcularHash(byte[] ultimoHash, HistoricoEvento evento)
    {
        using var sha = SHA256.Create();
        var ms = new System.IO.MemoryStream();
        ms.Write(ultimoHash);
        var bytesRestantes = Encoding.UTF8.GetBytes(
            $"{evento.ExecucaoId}{evento.OcorridoEm}{evento.Tipo}{evento.Origem ?? ""}{evento.Destino ?? ""}{evento.DetalhesJson}"
        );
        ms.Write(bytesRestantes);
        ms.Position = 0;
        return sha.ComputeHash(ms);
    }
}
