using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System.Text.RegularExpressions;

namespace FolderOrganIAzer.Host;

public class FormatadorComChaveOculta : ITextFormatter
{
    private readonly ITextFormatter _innerFormatter;
    private static readonly Regex ChaveRegex = new Regex(@"sk-ant-[A-Za-z0-9_\-]+", RegexOptions.Compiled);

    public FormatadorComChaveOculta(ITextFormatter innerFormatter)
    {
        _innerFormatter = innerFormatter;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var tempWriter = new StringWriter();
        _innerFormatter.Format(logEvent, tempWriter);
        var message = tempWriter.ToString();
        var safeMessage = ChaveRegex.Replace(message, "[CHAVE]");
        output.Write(safeMessage);
    }
}
