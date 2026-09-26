using FolderOrganIAzer.Persistencia;
using Serilog;
using Serilog.Formatting.Display;
using System.Security.Principal;

namespace FolderOrganIAzer.Host;

public class Program
{
    public static int Main(string[] args)
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? "default";
        var mutexName = $@"Global\FolderOrganIAzer-{sid}";
        
        using var mutex = new Mutex(true, mutexName, out bool isNew);
        if (!isNew)
        {
            Console.WriteLine("Já existe uma instância do FolderOrganIAzer em execução para este usuário.");
            return 1;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseDir = Path.Combine(appData, "FolderOrganIAzer");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--pasta-dados")
            {
                baseDir = args[i + 1];
                break;
            }
        }

        var logsDir = Path.Combine(baseDir, "logs");
        var dadosDir = Path.Combine(baseDir, "dados");

        var innerFormatter = new MessageTemplateTextFormatter(
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        var formatter = new FormatadorComChaveOculta(innerFormatter);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(formatter)
            .WriteTo.File(
                formatter,
                Path.Combine(logsDir, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("Iniciando FolderOrganIAzer...");

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            var pipeName = $"FolderOrganIAzer-{sid}";
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenNamedPipe(pipeName);
            });

            builder.Services.AddSingleton(new GerenciadorBanco(dadosDir));
            builder.Services.AddSingleton<GerenciadorConfiguracao>(sp => 
                new GerenciadorConfiguracao(dadosDir, sp.GetService<ILogger<GerenciadorConfiguracao>>()));

            var app = builder.Build();

            var gerenciadorBanco = app.Services.GetRequiredService<GerenciadorBanco>();
            gerenciadorBanco.GarantirEsquema();
            
            var configGerenciador = app.Services.GetRequiredService<GerenciadorConfiguracao>();
            configGerenciador.Carregar();

            app.MapGet("/v1/saude", (GerenciadorBanco db) =>
            {
                var versaoApp = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
                
                using var conexao = db.CriarConexao();
                using var cmdVersao = conexao.CreateCommand();
                cmdVersao.CommandText = "PRAGMA user_version;";
                var versaoEsquema = (long)(cmdVersao.ExecuteScalar() ?? 0L);

                using var cmdIntegridade = conexao.CreateCommand();
                cmdIntegridade.CommandText = "PRAGMA quick_check;";
                var check = cmdIntegridade.ExecuteScalar()?.ToString();
                bool bancoIntegro = check == "ok";

                return Results.Json(new
                {
                    versaoApp = versaoApp,
                    versaoEsquema = versaoEsquema,
                    bancoIntegro = bancoIntegro
                });
            });

            app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erro fatal no host.");
            return 2;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
