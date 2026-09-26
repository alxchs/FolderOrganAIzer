using System;
using System.IO;
using Xunit;
using FolderOrganIAzer.SistemaArquivos;

namespace FolderOrganIAzer.Testes.Unidade;

public class CaminhoEstendidoTestes
{
    [Fact]
    public void CaminhoRelativo()
    {
        string relativo = "pasta/arquivo.txt";
        string esperado = @"\\?\" + Path.GetFullPath(relativo);
        string resultado = CaminhoEstendido.De(relativo);
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void CaminhoComDoisPontos()
    {
        string caminho = @"C:\pasta\subpasta\..\arquivo.txt";
        string esperado = @"\\?\C:\pasta\arquivo.txt";
        string resultado = CaminhoEstendido.De(caminho);
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void CaminhoComBarraNormal()
    {
        string caminho = "C:/pasta/arquivo.txt";
        string esperado = @"\\?\C:\pasta\arquivo.txt";
        string resultado = CaminhoEstendido.De(caminho);
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void CaminhoJaPrefixadoLocal()
    {
        string caminho = @"\\?\C:\pasta\arquivo.txt";
        string resultado = CaminhoEstendido.De(caminho);
        Assert.Equal(caminho, resultado);
    }

    [Fact]
    public void CaminhoJaPrefixadoDeRede()
    {
        string caminho = @"\\?\UNC\servidor\pasta\arquivo.txt";
        string resultado = CaminhoEstendido.De(caminho);
        Assert.Equal(caminho, resultado);
    }

    [Fact]
    public void CaminhoDeRede()
    {
        string caminho = @"\\servidor\pasta\arquivo.txt";
        string esperado = @"\\?\UNC\servidor\pasta\arquivo.txt";
        string resultado = CaminhoEstendido.De(caminho);
        Assert.Equal(esperado, resultado);
    }
}
