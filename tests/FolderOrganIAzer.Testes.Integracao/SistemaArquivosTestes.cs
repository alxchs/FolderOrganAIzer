using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FolderOrganIAzer.SistemaArquivos;

namespace FolderOrganIAzer.Testes.Integracao;

public class SistemaArquivosTestes : IDisposable
{
    private readonly string _pastaRaizTeste;

    public SistemaArquivosTestes()
    {
        _pastaRaizTeste = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Integracao", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_pastaRaizTeste);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pastaRaizTeste))
        {
            Directory.Delete(_pastaRaizTeste, true);
        }
    }

    [Fact]
    public void IdentidadeIgual_AntesEDepoisDeMover()
    {
        string origem = Path.Combine(_pastaRaizTeste, "arquivo_movido.txt");
        string destino = Path.Combine(_pastaRaizTeste, "arquivo_destino.txt");
        File.WriteAllText(origem, "teste");

        var infoOrigem = OperacoesDisco.LerInfo(origem);
        
        var resultado = OperacoesDisco.MoverArquivo(origem, destino);
        Assert.True(resultado.Sucesso);

        var infoDestino = OperacoesDisco.LerInfo(destino);

        Assert.Equal(infoOrigem.Identidade, infoDestino.Identidade);
    }

    [Fact]
    public void MoverArquivo_RecusaSobrescrever()
    {
        string origem = Path.Combine(_pastaRaizTeste, "origem.txt");
        string destino = Path.Combine(_pastaRaizTeste, "destino.txt");
        File.WriteAllText(origem, "A");
        File.WriteAllText(destino, "B");

        var resultado = OperacoesDisco.MoverArquivo(origem, destino);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErroMovimento.JaExiste, resultado.Erro);
    }

    [Fact]
    public void CriarPasta_FalhaSemPastaMae()
    {
        string caminho = Path.Combine(_pastaRaizTeste, "Nivel1", "Nivel2");
        
        Assert.Throws<System.ComponentModel.Win32Exception>(() => OperacoesDisco.CriarPasta(caminho));
    }

    [Fact]
    public void Arquivo_NomeTerminadoEmPonto_AcessivelComPrefixo()
    {
        string nome = "arquivo_com_ponto.";
        string caminho = Path.Combine(_pastaRaizTeste, nome);
        string caminhoLongo = CaminhoEstendido.De(caminho);
        
        using (var fs = new FileStream(caminhoLongo, FileMode.Create))
        {
            fs.WriteByte(1);
        }

        var info = OperacoesDisco.LerInfo(caminho);
        Assert.Equal(1, info.Tamanho);
    }

    [Fact]
    public void LocalizarArquivoMovido_PelaIdentidade()
    {
        string origem = Path.Combine(_pastaRaizTeste, "para_localizar.txt");
        string destino = Path.Combine(_pastaRaizTeste, "localizado.txt");
        File.WriteAllText(origem, "localiza-me");

        var infoOrigem = OperacoesDisco.LerInfo(origem);
        
        OperacoesDisco.MoverArquivo(origem, destino);

        string? caminhoEncontrado = OperacoesDisco.LocalizarArquivo(_pastaRaizTeste, infoOrigem.Identidade);

        Assert.NotNull(caminhoEncontrado);
        
        string destinoLongo = CaminhoEstendido.De(destino);
        Assert.Equal(destinoLongo, caminhoEncontrado, ignoreCase: true);
    }

    [Fact]
    public void LocalizarIdentidadeDeArquivoApagado_DevolveNulo()
    {
        string origem = Path.Combine(_pastaRaizTeste, "para_apagar.txt");
        File.WriteAllText(origem, "apague-me");

        var infoOrigem = OperacoesDisco.LerInfo(origem);
        
        File.Delete(origem);

        string? caminhoEncontrado = OperacoesDisco.LocalizarArquivo(_pastaRaizTeste, infoOrigem.Identidade);

        Assert.Null(caminhoEncontrado);
    }

    [Fact]
    public void Marcador_GravadoERecuperado()
    {
        var marcador = new MarcadorPasta
        {
            Categoria = "Testes",
            ExecucaoId = Guid.NewGuid().ToString(),
            CriadaEm = DateTimeOffset.UtcNow,
            Guid = Guid.NewGuid(),
            Nivel = 1,
            RaizNaCriacao = _pastaRaizTeste
        };

        MarcadorOrganizador.GravarMarcador(_pastaRaizTeste, marcador);

        string arquivoMarcador = Path.Combine(_pastaRaizTeste, ".organizador");
        var attributes = File.GetAttributes(arquivoMarcador);
        
        Assert.True(attributes.HasFlag(FileAttributes.Hidden));
        Assert.True(attributes.HasFlag(FileAttributes.System));

        var lido = MarcadorOrganizador.LerMarcador(_pastaRaizTeste);
        
        Assert.NotNull(lido);
        Assert.Equal(marcador.Guid, lido!.Guid);
    }

    [TesteRequerMultiplosVolumes]
    public void MoverArquivo_Erro17_VolumesDiferentes()
    {
        string origem = Path.Combine(_pastaRaizTeste, "origem_vol.txt");
        File.WriteAllText(origem, "A");

        DriveInfo outraUnidade = null!;
        string raizAtual = Path.GetPathRoot(_pastaRaizTeste) ?? "C:\\";

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Fixed && drive.IsReady && drive.AvailableFreeSpace > 1024 * 1024 &&
                !string.Equals(drive.Name, raizAtual, StringComparison.OrdinalIgnoreCase))
            {
                outraUnidade = drive;
                break;
            }
        }

        string pastaOutroVolume = Path.Combine(outraUnidade.Name, "FolderOrganIAzer_Teste_Vol", Guid.NewGuid().ToString());
        Directory.CreateDirectory(pastaOutroVolume);

        try
        {
            string destino = Path.Combine(pastaOutroVolume, "destino_vol.txt");
            var resultado = OperacoesDisco.MoverArquivo(origem, destino);

            Assert.False(resultado.Sucesso);
            Assert.Equal(ErroMovimento.VolumeDiferente, resultado.Erro);
        }
        finally
        {
            Directory.Delete(pastaOutroVolume, true);
        }
    }
}

public class TesteRequerMultiplosVolumesAttribute : FactAttribute
{
    public TesteRequerMultiplosVolumesAttribute()
    {
        var tempDir = Path.GetTempPath();
        var unidadePrincipal = new DriveInfo(Path.GetPathRoot(tempDir) ?? "C:\\");
        
        bool encontrou = false;
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady && drive.DriveType == DriveType.Fixed && 
                !string.Equals(drive.Name, unidadePrincipal.Name, StringComparison.OrdinalIgnoreCase) && 
                drive.AvailableFreeSpace > 1024 * 1024)
            {
                encontrou = true;
                break;
            }
        }
        
        if (!encontrou)
        {
            Skip = "Não há segunda unidade local fixa com espaço disponível.";
        }
    }
}
