using Xunit;
using FolderOrganIAzer.Host;
using Serilog.Events;
using Serilog.Parsing;
using System.IO;
using System;

namespace FolderOrganIAzer.Testes.Unidade;

public class FormatadorComChaveOcultaTestes
{
    [Fact]
    public void FiltraChaveNaMensagem()
    {
        var formatadorInterno = new FormatadorFalso();
        var formatador = new FormatadorComChaveOculta(formatadorInterno);

        var eventoLog = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate(new[] { new TextToken("Minha chave e sk-ant-12345-abcde e outra sk-ant-xyz") }),
            Array.Empty<LogEventProperty>());

        using var escritor = new StringWriter();
        formatador.Format(eventoLog, escritor);

        var saida = escritor.ToString();

        Assert.Contains("Minha chave e [CHAVE] e outra [CHAVE]", saida);
        Assert.DoesNotContain("sk-ant-12345-abcde", saida);
    }

    private class FormatadorFalso : Serilog.Formatting.ITextFormatter
    {
        public void Format(LogEvent eventoLog, TextWriter saida)
        {
            saida.Write(eventoLog.MessageTemplate.Text);
        }
    }
}
