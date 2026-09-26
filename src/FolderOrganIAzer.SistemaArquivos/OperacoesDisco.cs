using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace FolderOrganIAzer.SistemaArquivos;

public enum ErroMovimento
{
    EmUso,
    SemPermissao,
    JaExiste,
    Desaparecido,
    VolumeDiferente,
    VolumeIndisponivel,
    CaminhoNaoEncontrado,
    Outro
}

public class ResultadoMovimento
{
    public bool Sucesso { get; set; }
    public ErroMovimento? Erro { get; set; }
    public string? MensagemErro { get; set; }
}

public record IdentidadeArquivo(ulong VolumeSerial, Guid FileId);

public record InfoArquivo(
    IdentidadeArquivo Identidade,
    long Tamanho,
    DateTimeOffset CriadoEm,
    DateTimeOffset ModificadoEm,
    uint Atributos,
    uint NumeroVinculos,
    uint EtiquetaPontoReanalise);

public static class OperacoesDisco
{
    public static unsafe InfoArquivo LerInfo(string caminho, bool ehPasta = false)
    {
        string caminhoLongo = CaminhoEstendido.De(caminho);
        
        FILE_FLAGS_AND_ATTRIBUTES flags = FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT;
        if (ehPasta)
        {
            flags |= FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS;
        }

        using var handle = PInvoke.CreateFile(
            caminhoLongo,
            0,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            flags,
            null);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        FILE_ID_INFO idInfo;
        if (!PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, &idInfo, (uint)sizeof(FILE_ID_INFO)))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        FILE_BASIC_INFO basicInfo;
        if (!PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, &basicInfo, (uint)sizeof(FILE_BASIC_INFO)))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        FILE_STANDARD_INFO standardInfo;
        if (!PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, &standardInfo, (uint)sizeof(FILE_STANDARD_INFO)))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        uint reparseTag = 0;
        if ((basicInfo.FileAttributes & (uint)FileAttributes.ReparsePoint) != 0)
        {
            FILE_ATTRIBUTE_TAG_INFO tagInfo;
            if (PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileAttributeTagInfo, &tagInfo, (uint)sizeof(FILE_ATTRIBUTE_TAG_INFO)))
            {
                reparseTag = tagInfo.ReparseTag;
            }
        }

        var bytes = new byte[16];
        for (int i = 0; i < 16; i++) bytes[i] = idInfo.FileId.Identifier[i];
        var guidId = new Guid(bytes);
        
        return new InfoArquivo(
            new IdentidadeArquivo(idInfo.VolumeSerialNumber, guidId),
            standardInfo.EndOfFile,
            DateTimeOffset.FromFileTime(basicInfo.CreationTime),
            DateTimeOffset.FromFileTime(basicInfo.LastWriteTime),
            basicInfo.FileAttributes,
            standardInfo.NumberOfLinks,
            reparseTag
        );
    }

    public static unsafe (string TipoUnidade, string SistemaArquivos) ObterInfoVolume(string caminho)
    {
        string raiz = Path.GetPathRoot(caminho) ?? string.Empty;
        if (!raiz.EndsWith('\\')) raiz += '\\';

        uint driveType = PInvoke.GetDriveType(raiz);
        string tipo = driveType switch
        {
            2 => "Removível",
            3 => "Fixo",
            4 => "Rede",
            5 => "CD/DVD",
            6 => "RAM",
            _ => "Desconhecido"
        };

        string caminhoLongo = CaminhoEstendido.De(raiz);
        using var handle = PInvoke.CreateFile(
            caminhoLongo,
            0,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
            null);

        if (handle.IsInvalid)
            return (tipo, "Desconhecido");

        var nomeFs = new char[MAX_PATH + 1];
        uint serial;
        uint maxComponentLength;
        uint fileSystemFlags;
        
        fixed (char* fsPtr = nomeFs)
        {
            if (PInvoke.GetVolumeInformationByHandle(handle, null, 0, &serial, &maxComponentLength, &fileSystemFlags, fsPtr, (uint)nomeFs.Length))
            {
                string fs = new string(fsPtr);
                return (tipo, fs);
            }
        }

        return (tipo, "Desconhecido");
    }

    private const int MAX_PATH = 260;

    public static void CriarPasta(string caminho)
    {
        string caminhoLongo = CaminhoEstendido.De(caminho);
        if (!PInvoke.CreateDirectory(caminhoLongo, null))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static ResultadoMovimento MoverArquivo(string origem, string destino)
    {
        string origemLongo = CaminhoEstendido.De(origem);
        string destinoLongo = CaminhoEstendido.De(destino);

        if (PInvoke.MoveFileEx(origemLongo, destinoLongo, 0))
        {
            return new ResultadoMovimento { Sucesso = true };
        }

        const int ErroCompartilhamento = 32;
        const int ErroBloqueio = 33;
        const int ErroArquivoJaExiste = 183;
        const int ErroArquivoExiste = 80;
        const int ErroAcessoNegado = 5;
        const int ErroArquivoNaoEncontrado = 2;
        const int ErroCaminhoNaoEncontrado = 3;
        const int ErroVolumeDiferente = 17;
        const int ErroDispositivoNaoConectado = 1167;
        const int ErroNaoPronto = 21;

        int erro = Marshal.GetLastWin32Error();
        return new ResultadoMovimento
        {
            Sucesso = false,
            Erro = erro switch
            {
                ErroCompartilhamento or ErroBloqueio => ErroMovimento.EmUso,
                ErroArquivoJaExiste or ErroArquivoExiste => ErroMovimento.JaExiste,
                ErroAcessoNegado => ErroMovimento.SemPermissao,
                ErroArquivoNaoEncontrado => ErroMovimento.Desaparecido,
                ErroCaminhoNaoEncontrado => ErroMovimento.CaminhoNaoEncontrado,
                ErroVolumeDiferente => ErroMovimento.VolumeDiferente,
                ErroDispositivoNaoConectado or ErroNaoPronto => ErroMovimento.VolumeIndisponivel,
                _ => ErroMovimento.Outro
            },
            MensagemErro = new Win32Exception(erro).Message
        };
    }

    public static void RemoverPastaVazia(string caminho)
    {
        string caminhoLongo = CaminhoEstendido.De(caminho);
        if (!PInvoke.RemoveDirectory(caminhoLongo))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static unsafe string? LocalizarArquivo(string raizVolume, IdentidadeArquivo identidade)
    {
        string raizLongo = CaminhoEstendido.De(raizVolume);

        using var handleVolume = PInvoke.CreateFile(
            raizLongo,
            0,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
            null);

        if (handleVolume.IsInvalid) return null;

        FILE_ID_DESCRIPTOR desc = new()
        {
            dwSize = (uint)sizeof(FILE_ID_DESCRIPTOR),
            Type = FILE_ID_TYPE.ExtendedFileIdType
        };
        var idBytes = identidade.FileId.ToByteArray();
        for (int i = 0; i < 16; i++) desc.Anonymous.ExtendedFileId.Identifier[i] = idBytes[i];

        using var handleArquivo = PInvoke.OpenFileById(
            handleVolume,
            in desc,
            0,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
            null,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS);

        if (!handleArquivo.IsInvalid)
        {
            return LerCaminhoFinal(handleArquivo);
        }

        desc.Type = FILE_ID_TYPE.FileIdType;
        desc.Anonymous.FileId = BitConverter.ToInt64(idBytes, 0);

        using var handleFallback = PInvoke.OpenFileById(
            handleVolume,
            in desc,
            0,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
            null,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS);

        if (!handleFallback.IsInvalid) 
        {
            return LerCaminhoFinal(handleFallback);
        }

        return null;
    }

    private static unsafe string? LerCaminhoFinal(SafeHandle handle)
    {
        var pathBuffer = new char[32767];
        fixed (char* p = pathBuffer)
        {
            uint len = PInvoke.GetFinalPathNameByHandle(handle, p, (uint)pathBuffer.Length, 0);
            if (len > 0 && len < pathBuffer.Length)
            {
                return new string(p, 0, (int)len);
            }
        }
        return null;
    }
}
