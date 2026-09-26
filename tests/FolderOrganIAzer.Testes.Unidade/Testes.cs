using Xunit;
using FolderOrganIAzer.Dominio.Configuracao;
using FolderOrganIAzer.Persistencia;
using System.IO;
using Serilog.Events;
using Serilog.Parsing;
using FolderOrganIAzer.Host;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
        var tempDir = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = new GerenciadorConfiguracao(tempDir);
            var configPadrao = new ConfiguracaoAplicacao();

            var carregado = manager.Carregar();

            Assert.True(File.Exists(Path.Combine(tempDir, "config.json")));
            Assert.Equal(configPadrao.LimiarConfianca, carregado.LimiarConfianca);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Carregar_JSONCorrompido_UsaPadroesERegistraAviso()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "config.json"), "{ invalid_json }");
            var logger = new LoggerDeTeste();
            var manager = new GerenciadorConfiguracao(tempDir, logger);
            var configPadrao = new ConfiguracaoAplicacao();

            var carregado = manager.Carregar();

            Assert.Equal(configPadrao.LimiarConfianca, carregado.LimiarConfianca);
            Assert.Equal(configPadrao.TetoCustoUsd, carregado.TetoCustoUsd);
            Assert.True(logger.AvisoRegistrado);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Carregar_ValoresForaDaFaixa_UsaPadroes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = new GerenciadorConfiguracao(tempDir);
            var configPadrao = new ConfiguracaoAplicacao();
            
            var badConfig = new ConfiguracaoAplicacao
            {
                LimiarConfianca = 1.5m,
                ConcorrenciaIA = 20,
                TempoLimitePortaoSegundos = -10,
                PoliticaPendencias = "INVENTADA"
            };

            manager.Salvar(badConfig);
            var carregado = manager.Carregar();

            Assert.Equal(configPadrao.LimiarConfianca, carregado.LimiarConfianca);
            Assert.Equal(configPadrao.ConcorrenciaIA, carregado.ConcorrenciaIA);
            Assert.Equal(configPadrao.TempoLimitePortaoSegundos, carregado.TempoLimitePortaoSegundos);
            Assert.Equal(configPadrao.PoliticaPendencias, carregado.PoliticaPendencias);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Carregar_PrecoNegativo_Invalido()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_Testes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = new GerenciadorConfiguracao(tempDir);
            
            var badConfig = new ConfiguracaoAplicacao
            {
                PrecosModelos = new List<PrecoModelo> 
                { 
                    new PrecoModelo { EntradaUsdPorMilhao = -10, SaidaUsdPorMilhao = -5, LeituraCacheUsdPorMilhao = -1 } 
                }
            };

            manager.Salvar(badConfig);
            var carregado = manager.Carregar();

            Assert.Equal(0, carregado.PrecosModelos[0].EntradaUsdPorMilhao);
            Assert.Equal(0, carregado.PrecosModelos[0].SaidaUsdPorMilhao);
            Assert.Equal(0, carregado.PrecosModelos[0].LeituraCacheUsdPorMilhao);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

public class FiltroChaveTestes
{
    [Fact]
    public void FiltraChaveNaMensagem()
    {
        var inner = new DummyFormatter();
        var formatter = new FormatadorComChaveOculta(inner);

        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate(new[] { new TextToken("Minha chave e sk-ant-12345-abcde e outra sk-ant-xyz") }),
            Array.Empty<LogEventProperty>());

        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);

        var output = writer.ToString();

        Assert.Contains("Minha chave e [CHAVE] e outra [CHAVE]", output);
        Assert.DoesNotContain("sk-ant-12345-abcde", output);
    }

    private class DummyFormatter : Serilog.Formatting.ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            output.Write(logEvent.MessageTemplate.Text);
        }
    }
}
