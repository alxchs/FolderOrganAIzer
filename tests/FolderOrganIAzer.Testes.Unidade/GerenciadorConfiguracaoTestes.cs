using Xunit;
using FolderOrganIAzer.Dominio.Configuracao;
using System.IO;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using FolderOrganIAzer.Persistencia;

namespace FolderOrganIAzer.Testes.Unidade;

public class GerenciadorConfiguracaoTestes
{
    private class LoggerDeTeste : ILogger<GerenciadorConfiguracao>
    {
        public bool AvisoRegistrado { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                AvisoRegistrado = true;
            }
        }
    }

    [Fact]
    public void Carregar_ArquivoAusente_CriaComPadroes()
    {
        var pastaTemp = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(pastaTemp);
        try
        {
            var gerenciador = new GerenciadorConfiguracao(pastaTemp);
            var configuracaoPadrao = new ConfiguracaoAplicacao();

            var carregado = gerenciador.Carregar();

            Assert.True(File.Exists(Path.Combine(pastaTemp, "config.json")));
            Assert.Equal(configuracaoPadrao.LimiarConfianca, carregado.LimiarConfianca);
        }
        finally
        {
            Directory.Delete(pastaTemp, true);
        }
    }

    [Fact]
    public void Carregar_JSONCorrompido_UsaPadroesERegistraAviso()
    {
        var pastaTemp = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(pastaTemp);
        try
        {
            File.WriteAllText(Path.Combine(pastaTemp, "config.json"), "{ invalid_json }");
            var registrador = new LoggerDeTeste();
            var gerenciador = new GerenciadorConfiguracao(pastaTemp, registrador);
            var configuracaoPadrao = new ConfiguracaoAplicacao();

            var carregado = gerenciador.Carregar();

            Assert.Equal(configuracaoPadrao.LimiarConfianca, carregado.LimiarConfianca);
            Assert.Equal(configuracaoPadrao.TetoCustoUsd, carregado.TetoCustoUsd);
            Assert.True(registrador.AvisoRegistrado);
        }
        finally
        {
            Directory.Delete(pastaTemp, true);
        }
    }

    [Fact]
    public void Carregar_ValoresForaDaFaixa_UsaPadroes()
    {
        var pastaTemp = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(pastaTemp);
        try
        {
            var gerenciador = new GerenciadorConfiguracao(pastaTemp);
            var configuracaoPadrao = new ConfiguracaoAplicacao();
            
            var configuracaoRuim = new ConfiguracaoAplicacao
            {
                LimiarConfianca = 1.5m,
                ConcorrenciaIA = 20,
                TempoLimitePortaoSegundos = -10,
                PoliticaPendencias = "INVENTADA"
            };

            gerenciador.Salvar(configuracaoRuim);
            var carregado = gerenciador.Carregar();

            Assert.Equal(configuracaoPadrao.LimiarConfianca, carregado.LimiarConfianca);
            Assert.Equal(configuracaoPadrao.ConcorrenciaIA, carregado.ConcorrenciaIA);
            Assert.Equal(configuracaoPadrao.TempoLimitePortaoSegundos, carregado.TempoLimitePortaoSegundos);
            Assert.Equal(configuracaoPadrao.PoliticaPendencias, carregado.PoliticaPendencias);
        }
        finally
        {
            Directory.Delete(pastaTemp, true);
        }
    }

    [Fact]
    public void Carregar_PrecoNegativo_Invalido()
    {
        var pastaTemp = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(pastaTemp);
        try
        {
            var gerenciador = new GerenciadorConfiguracao(pastaTemp);
            
            var configuracaoRuim = new ConfiguracaoAplicacao
            {
                PrecosModelos = new List<PrecoModelo> 
                { 
                    new PrecoModelo { EntradaUsdPorMilhao = -10, SaidaUsdPorMilhao = -5, LeituraCacheUsdPorMilhao = -1 } 
                }
            };

            gerenciador.Salvar(configuracaoRuim);
            var carregado = gerenciador.Carregar();

            Assert.Equal(0, carregado.PrecosModelos[0].EntradaUsdPorMilhao);
            Assert.Equal(0, carregado.PrecosModelos[0].SaidaUsdPorMilhao);
            Assert.Equal(0, carregado.PrecosModelos[0].LeituraCacheUsdPorMilhao);
        }
        finally
        {
            Directory.Delete(pastaTemp, true);
        }
    }
}
