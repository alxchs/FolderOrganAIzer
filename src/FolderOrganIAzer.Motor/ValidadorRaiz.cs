using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FolderOrganIAzer.Dominio;
using FolderOrganIAzer.Persistencia;
using FolderOrganIAzer.SistemaArquivos;

namespace FolderOrganIAzer.Motor;

public class ValidadorRaiz
{
    private readonly IRepositorioExecucao _repositorioExecucao;
    private readonly IProvedorIA _provedorIA;
    private readonly IProvedorVolume _provedorVolume;

    public ValidadorRaiz(IRepositorioExecucao repositorioExecucao, IProvedorIA provedorIA, IProvedorVolume provedorVolume)
    {
        _repositorioExecucao = repositorioExecucao;
        _provedorIA = provedorIA;
        _provedorVolume = provedorVolume;
    }

    public async Task<List<AchadoValidacao>> ValidarAsync(string caminhoRaiz, bool modoSemIa = false, string? ignorarExecucaoId = null)
    {
        var achados = new List<AchadoValidacao>();

        
        if (!Directory.Exists(caminhoRaiz))
        {
            achados.Add(new AchadoValidacao { Codigo = "RAIZ_INEXISTENTE", Severidade = SeveridadeAchado.BLOQUEIO });
            return achados;
        }

        
        string caminhoReal = ObterCaminhoReal(caminhoRaiz) ?? caminhoRaiz;

        
        
        FolderOrganIAzer.SistemaArquivos.InfoArquivo info;
        try
        {
            info = OperacoesDisco.LerInfo(caminhoRaiz, ehPasta: true);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is UnauthorizedAccessException)
        {
            achados.Add(new AchadoValidacao { Codigo = "SEM_PERMISSAO_ESCRITA", Severidade = SeveridadeAchado.BLOQUEIO });
            return achados;
        }

        if ((info.Atributos & (uint)FileAttributes.ReparsePoint) != 0)
        {
            achados.Add(new AchadoValidacao 
            { 
                Codigo = "RAIZ_E_LINK", 
                Severidade = SeveridadeAchado.BLOQUEIO,
                Mensagem = $"A raiz fornecida é um link. Use o caminho real: {caminhoReal}"
            });
            return achados;
        }

        
        var infoVolume = _provedorVolume.ObterInfoVolume(caminhoReal);
        if (infoVolume.TipoUnidade is not "Fixo" and not "Removível")
        {
            achados.Add(new AchadoValidacao { Codigo = "VOLUME_NAO_SUPORTADO", Severidade = SeveridadeAchado.BLOQUEIO });
            return achados;
        }

        
        if (infoVolume.SistemaArquivos is not "NTFS" and not "ReFS")
        {
            achados.Add(new AchadoValidacao { Codigo = "SISTEMA_ARQUIVOS_NAO_SUPORTADO", Severidade = SeveridadeAchado.BLOQUEIO });
            return achados;
        }

        
        if (EhLocalProtegido(caminhoReal))
        {
            achados.Add(new AchadoValidacao { Codigo = "LOCAL_PROTEGIDO", Severidade = SeveridadeAchado.BLOQUEIO });
            return achados;
        }

        
        var execucoesInacabadas = _repositorioExecucao.ListarExecucoesAtivas().ToList();
        var execucoesRodando = execucoesInacabadas.Where(e => e.Estado != EstadoExecucao.PAUSADA && e.Id != ignorarExecucaoId).ToList();

        foreach (var execucao in execucoesRodando)
        {
            var raizExec = _repositorioExecucao.ObterRaiz(execucao.RaizId);
            if (raizExec != null)
            {
                if (caminhoReal.Equals(raizExec.Caminho, StringComparison.OrdinalIgnoreCase) ||
                    caminhoReal.StartsWith(raizExec.Caminho + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    raizExec.Caminho.StartsWith(caminhoReal + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    achados.Add(new AchadoValidacao { Codigo = "RAIZ_EM_USO", Severidade = SeveridadeAchado.BLOQUEIO });
                    return achados;
                }
            }
        }

        
        if (!TemPermissaoEscrita(caminhoReal))
        {
            achados.Add(new AchadoValidacao { Codigo = "SEM_PERMISSAO_ESCRITA", Severidade = SeveridadeAchado.BLOQUEIO });
            return achados;
        }

        
        var achadoPastaGerada = VerificarPastaGerada(caminhoReal);
        if (achadoPastaGerada != null)
        {
            achados.Add(achadoPastaGerada);
        }

        
        if (EhPastaDeProjeto(caminhoReal))
        {
            achados.Add(new AchadoValidacao { Codigo = "PASTA_DE_PROJETO", Severidade = SeveridadeAchado.CONFIRMACAO });
        }

        
        var execucaoInacabada = execucoesInacabadas.FirstOrDefault(e => 
        {
            var r = _repositorioExecucao.ObterRaiz(e.RaizId);
            return r != null && r.Caminho.Equals(caminhoReal, StringComparison.OrdinalIgnoreCase);
        });

        if (execucaoInacabada != null)
        {
            achados.Add(new AchadoValidacao 
            { 
                Codigo = "EXECUCAO_INACABADA", 
                Severidade = SeveridadeAchado.CONFIRMACAO,
                Detalhes = new { execucaoId = execucaoInacabada.Id }
            });
        }

        
        bool iaResponde = await _provedorIA.TestarDisponibilidadeAsync();
        if (!iaResponde)
        {
            achados.Add(new AchadoValidacao 
            { 
                Codigo = "IA_INDISPONIVEL", 
                Severidade = modoSemIa ? SeveridadeAchado.AVISO : SeveridadeAchado.BLOQUEIO 
            });
        }

        
        string raizUnidade = Path.GetPathRoot(caminhoRaiz) ?? string.Empty;
        string unidadeSistema = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? string.Empty;

        if (raizUnidade.TrimEnd(Path.DirectorySeparatorChar).Equals(
                caminhoRaiz.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
            && !raizUnidade.Equals(unidadeSistema, StringComparison.OrdinalIgnoreCase))
        {
            achados.Add(new AchadoValidacao { Codigo = "RAIZ_DE_UNIDADE", Severidade = SeveridadeAchado.AVISO });
        }

        if (caminhoReal.Length > 180)
        {
            achados.Add(new AchadoValidacao { Codigo = "CAMINHO_LONGO", Severidade = SeveridadeAchado.AVISO });
        }

        if (_provedorVolume.ObterEspacoLivreAppData() < 500L * 1024 * 1024)
        {
            achados.Add(new AchadoValidacao { Codigo = "POUCO_ESPACO_DADOS", Severidade = SeveridadeAchado.AVISO });
        }

        return achados;
    }

    private string? ObterCaminhoReal(string caminho)
    {
        return OperacoesDisco.ObterCaminhoReal(caminho);
    }

    private bool EhLocalProtegido(string caminho)
    {
        var locais = new List<string>();

        
        string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))!;
        if (caminho.Equals(systemDrive, StringComparison.OrdinalIgnoreCase) || caminho.Equals(systemDrive.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) return true;

        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar); 
        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        locais.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        
        string appDataLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FolderOrganIAzer");
        locais.Add(appDataLocal);

        caminho = caminho.TrimEnd(Path.DirectorySeparatorChar);

        if (caminho.Equals(systemDrive.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) return true;
        if (caminho.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) return true;

        foreach (var local in locais)
        {
            if (string.IsNullOrEmpty(local)) continue;
            var localTrimmed = local.TrimEnd(Path.DirectorySeparatorChar);
            
            
            if (localTrimmed.Equals(systemDrive.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) continue;
            if (localTrimmed.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) continue;

            if (caminho.Equals(localTrimmed, StringComparison.OrdinalIgnoreCase) ||
                caminho.StartsWith(localTrimmed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool TemPermissaoEscrita(string caminho)
    {
        string pastaTeste = Path.Combine(caminho, $".organizador-teste-{Guid.NewGuid()}");
        try
        {
            OperacoesDisco.CriarPasta(pastaTeste);
            File.SetAttributes(pastaTeste, FileAttributes.Hidden);
            OperacoesDisco.RemoverPastaVazia(pastaTeste);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private AchadoValidacao? VerificarPastaGerada(string caminho)
    {
        
        var marcador = MarcadorOrganizador.LerMarcador(caminho);
        if (marcador != null)
        {
            return new AchadoValidacao
            {
                Codigo = "RAIZ_E_PASTA_GERADA",
                Severidade = SeveridadeAchado.CONFIRMACAO,
                Detalhes = new 
                {
                    sinal = "MARCADOR",
                    pastaGerada = caminho,
                    nivel = marcador.Nivel,
                    raizOriginal = marcador.RaizNaCriacao,
                    criadaEm = FormatadorData.Formatar(marcador.CriadaEm.UtcDateTime),
                    criadaPelaExecucao = marcador.ExecucaoId
                }
            };
        }

        
        var info = OperacoesDisco.LerInfo(caminho, ehPasta: true);
        var pastaGerada = _repositorioExecucao.ObterPastaGeradaPorIdentidade((long)info.Identidade.VolumeSerial, info.Identidade.FileId.ToByteArray());
        if (pastaGerada != null && pastaGerada.RemovidaEm == null)
        {
            return new AchadoValidacao
            {
                Codigo = "RAIZ_E_PASTA_GERADA",
                Severidade = SeveridadeAchado.CONFIRMACAO,
                Detalhes = new 
                {
                    sinal = "REGISTRO",
                    pastaGerada = caminho,
                    nivel = pastaGerada.Nivel,
                    raizOriginal = pastaGerada.RaizCaminho,
                    criadaEm = pastaGerada.CriadaEm,
                    criadaPelaExecucao = pastaGerada.ExecucaoId
                }
            };
        }

        
        var pai = Path.GetDirectoryName(caminho);
        if (!string.IsNullOrEmpty(pai))
        {
            var achadoPai = VerificarAncestral(pai, caminho, 1);
            if (achadoPai != null) return achadoPai;

            var avo = Path.GetDirectoryName(pai);
            if (!string.IsNullOrEmpty(avo))
            {
                var achadoAvo = VerificarAncestral(avo, caminho, 2);
                if (achadoAvo != null) return achadoAvo;
            }
        }

        return null;
    }

    private AchadoValidacao? VerificarAncestral(string caminhoAncestral, string caminhoAtual, int distancia)
    {
        var marcador = MarcadorOrganizador.LerMarcador(caminhoAncestral);
        if (marcador != null)
        {
            return new AchadoValidacao
            {
                Codigo = "RAIZ_E_PASTA_GERADA",
                Severidade = SeveridadeAchado.CONFIRMACAO,
                Detalhes = new 
                {
                    sinal = "ANCESTRAL",
                    pastaGerada = caminhoAncestral,
                    nivel = marcador.Nivel,
                    raizOriginal = marcador.RaizNaCriacao,
                    criadaEm = FormatadorData.Formatar(marcador.CriadaEm.UtcDateTime),
                    criadaPelaExecucao = marcador.ExecucaoId
                }
            };
        }

        var info = OperacoesDisco.LerInfo(caminhoAncestral, ehPasta: true);
        var pastaGerada = _repositorioExecucao.ObterPastaGeradaPorIdentidade((long)info.Identidade.VolumeSerial, info.Identidade.FileId.ToByteArray());
        if (pastaGerada != null && pastaGerada.RemovidaEm == null)
        {
            return new AchadoValidacao
            {
                Codigo = "RAIZ_E_PASTA_GERADA",
                Severidade = SeveridadeAchado.CONFIRMACAO,
                Detalhes = new 
                {
                    sinal = "ANCESTRAL",
                    pastaGerada = caminhoAncestral,
                    nivel = pastaGerada.Nivel,
                    raizOriginal = pastaGerada.RaizCaminho,
                    criadaEm = pastaGerada.CriadaEm,
                    criadaPelaExecucao = pastaGerada.ExecucaoId
                }
            };
        }

        return null;
    }

    private bool EhPastaDeProjeto(string caminho)
    {
        var nomesCriticos = new[] { ".git", ".sln", ".csproj", ".dpr", "package.json", "pubspec.yaml" };
        try
        {
            var arquivos = Directory.EnumerateFileSystemEntries(caminho, "*", SearchOption.TopDirectoryOnly);
            foreach (var arq in arquivos)
            {
                var nome = Path.GetFileName(arq).ToLowerInvariant();
                if (nomesCriticos.Any(c => c == nome || nome.EndsWith(c)))
                {
                    return true;
                }
            }
        }
        catch { }
        return false;
    }
}
