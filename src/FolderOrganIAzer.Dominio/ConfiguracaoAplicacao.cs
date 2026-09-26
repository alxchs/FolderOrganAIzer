using System.Text.Json;

namespace FolderOrganIAzer.Dominio.Configuracao;

public class PrecoModelo
{
    public string Provedor { get; set; } = "";
    public string Modelo { get; set; } = "";
    public decimal EntradaUsdPorMilhao { get; set; }
    public decimal SaidaUsdPorMilhao { get; set; }
    public decimal LeituraCacheUsdPorMilhao { get; set; }
}

public class ConfiguracaoAplicacao
{
    public string Modelo { get; set; } = "claude-opus-5";
    public string ModoClassificacao { get; set; } = "Imediato";
    public decimal LimiarConfianca { get; set; } = 0.75m;
    public int ConcorrenciaIA { get; set; } = 4;
    public int ConcorrenciaExtracao { get; set; } = Math.Min(Environment.ProcessorCount, 8);
    public decimal TetoCustoUsd { get; set; } = 10m;
    public bool ModoSemIA { get; set; } = false;
    public bool ConsentimentoEnvioIA { get; set; } = false;
    public bool MascararDadosPessoais { get; set; } = true;
    public bool EnviarImagensParaIA { get; set; } = true;
    public int TempoLimitePortaoSegundos { get; set; } = 120;
    public bool RevisaoAutomatica { get; set; } = false;
    public string PoliticaPendencias { get; set; } = "MANTER_NA_RAIZ";
    public decimal ConfiancaMinimaPalpite { get; set; } = 0.50m;
    public bool IncluirOcultos { get; set; } = false;
    public int IdadeMinimaSegundos { get; set; } = 120;
    public bool BaixarArquivosSomenteOnline { get; set; } = false;
    public int MinimoArquivosParaNivel2 { get; set; } = 5;
    public int MinimoArquivosCategoriaNova { get; set; } = 3;
    public bool IncluirFormatoNoNivel2 { get; set; } = false;
    public bool RemoverAcentosDePastas { get; set; } = false;
    public int LimiteCaminho { get; set; } = 259;
    public int TamanhoLote { get; set; } = 200;
    public int MaxTentativasMovimento { get; set; } = 5;
    public int RetencaoTextoExtraidoDias { get; set; } = 90;

    public string ProvedorIA { get; set; } = "AGY";
    public string AgyCaminho { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "bin", "agy.exe");
    public string AgyModeloTentativa1 { get; set; } = "gemini-3.8-flash-low";
    public string AgyModeloTentativa2 { get; set; } = "gemini-3.1-pro-high";
    public int AgyTempoLimiteSegundos { get; set; } = 120;

    
    public List<PrecoModelo> PrecosModelos { get; set; } = new List<PrecoModelo>
    {
        new PrecoModelo { Provedor = "CLAUDE_API", Modelo = "claude-opus-5", EntradaUsdPorMilhao = 5.00m, SaidaUsdPorMilhao = 25.00m, LeituraCacheUsdPorMilhao = 0.50m },
        new PrecoModelo { Provedor = "CLAUDE_API", Modelo = "claude-sonnet-5", EntradaUsdPorMilhao = 2.00m, SaidaUsdPorMilhao = 10.00m, LeituraCacheUsdPorMilhao = 0.20m },
        new PrecoModelo { Provedor = "CLAUDE_API", Modelo = "claude-haiku-4-5", EntradaUsdPorMilhao = 1.00m, SaidaUsdPorMilhao = 5.00m, LeituraCacheUsdPorMilhao = 0.10m },
        new PrecoModelo { Provedor = "AGY", Modelo = "*", EntradaUsdPorMilhao = 0m, SaidaUsdPorMilhao = 0m, LeituraCacheUsdPorMilhao = 0m }
    };
}
