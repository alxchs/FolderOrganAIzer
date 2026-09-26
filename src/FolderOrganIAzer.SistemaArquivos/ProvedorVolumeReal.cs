using System;
using System.IO;
using FolderOrganIAzer.Dominio;

namespace FolderOrganIAzer.SistemaArquivos;

public class ProvedorVolumeReal : IProvedorVolume
{
    public (string TipoUnidade, string SistemaArquivos) ObterInfoVolume(string caminho)
    {
        return OperacoesDisco.ObterInfoVolume(caminho);
    }

    public long ObterEspacoLivreAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var drive = new DriveInfo(Path.GetPathRoot(localAppData)!);
        return drive.IsReady ? drive.AvailableFreeSpace : 0;
    }
}
