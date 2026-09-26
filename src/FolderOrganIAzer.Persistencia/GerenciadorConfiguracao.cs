using System.Text.Json;
using FolderOrganIAzer.Dominio.Configuracao;
using Microsoft.Extensions.Logging;

namespace FolderOrganIAzer.Persistencia;

public class GerenciadorConfiguracao
{
    private readonly string _caminhoConfig;
    private readonly ILogger<GerenciadorConfiguracao>? _logger;

    public GerenciadorConfiguracao(string caminhoPastaDados, ILogger<GerenciadorConfiguracao>? logger = null)
    {
        _caminhoConfig = Path.Combine(caminhoPastaDados, "config.json");
        _logger = logger;
    }

    public ConfiguracaoAplicacao Carregar()
    {
        if (!File.Exists(_caminhoConfig))
        {
            var configPadrao = new ConfiguracaoAplicacao();
            Salvar(configPadrao);
            return configPadrao;
        }

        try
        {
            var json = File.ReadAllText(_caminhoConfig);
            var config = JsonSerializer.Deserialize<ConfiguracaoAplicacao>(json) ?? new ConfiguracaoAplicacao();
            
            bool modificado = ValidarEArrumar(config);
            if (modificado)
            {
                _logger?.LogWarning("Configuracao fora da faixa ou corrompida. Alguns valores foram revertidos aos padroes.");
            }
            
            return config;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Erro ao ler config.json. Usando padroes.");
            return new ConfiguracaoAplicacao();
        }
    }

    public void Salvar(ConfiguracaoAplicacao config)
    {
        ValidarEArrumar(config);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_caminhoConfig, json);
    }

    private bool ValidarEArrumar(ConfiguracaoAplicacao config)
    {
        bool mudou = false;

        void Validar<T>(Func<T> obter, Action<T> definir, Func<T, bool> ehValido, T padrao)
        {
            if (!ehValido(obter()))
            {
                definir(padrao);
                mudou = true;
            }
        }

        Validar(() => config.ModoClassificacao, v => config.ModoClassificacao = v, v => v is "Imediato" or "Lote", "Imediato");
        Validar(() => config.LimiarConfianca, v => config.LimiarConfianca = v, v => v is >= 0.50m and <= 0.95m, 0.75m);
        Validar(() => config.ConcorrenciaIA, v => config.ConcorrenciaIA = v, v => v is >= 1 and <= 16, 4);
        Validar(() => config.ConcorrenciaExtracao, v => config.ConcorrenciaExtracao = v, v => v is >= 1 and <= 16, Math.Min(Environment.ProcessorCount, 8));
        Validar(() => config.TetoCustoUsd, v => config.TetoCustoUsd = v, v => v is >= 0m and <= 1000m, 10m);
        Validar(() => config.TempoLimitePortaoSegundos, v => config.TempoLimitePortaoSegundos = v, v => v is >= 0 and <= 3600, 120);
        Validar(() => config.PoliticaPendencias, v => config.PoliticaPendencias = v, v => v is "MANTER_NA_RAIZ" or "MOVER_PARA_DESCONHECIDOS" or "EXCLUIR", "MANTER_NA_RAIZ");
        Validar(() => config.ConfiancaMinimaPalpite, v => config.ConfiancaMinimaPalpite = v, v => v is >= 0.30m and <= 0.90m, 0.50m);
        Validar(() => config.IdadeMinimaSegundos, v => config.IdadeMinimaSegundos = v, v => v is >= 0 and <= 3600, 120);
        Validar(() => config.MinimoArquivosParaNivel2, v => config.MinimoArquivosParaNivel2 = v, v => v is >= 1 and <= 50, 5);
        Validar(() => config.MinimoArquivosCategoriaNova, v => config.MinimoArquivosCategoriaNova = v, v => v is >= 2 and <= 20, 3);
        Validar(() => config.LimiteCaminho, v => config.LimiteCaminho = v, v => v is >= 120 and <= 32000, 259);
        Validar(() => config.TamanhoLote, v => config.TamanhoLote = v, v => v is >= 1 and <= 1000, 200);
        Validar(() => config.MaxTentativasMovimento, v => config.MaxTentativasMovimento = v, v => v is >= 1 and <= 20, 5);
        Validar(() => config.RetencaoTextoExtraidoDias, v => config.RetencaoTextoExtraidoDias = v, v => v is >= 0 and <= 3650, 90);
        Validar(() => config.ProvedorIA, v => config.ProvedorIA = v, v => v is "AGY" or "CLAUDE_API", "AGY");
        Validar(() => config.AgyTempoLimiteSegundos, v => config.AgyTempoLimiteSegundos = v, v => v is >= 30 and <= 600, 120);

        if (config.PrecosModelos != null)
        {
            foreach (var p in config.PrecosModelos)
            {
                if (p.EntradaUsdPorMilhao < 0) { p.EntradaUsdPorMilhao = 0; mudou = true; }
                if (p.SaidaUsdPorMilhao < 0) { p.SaidaUsdPorMilhao = 0; mudou = true; }
                if (p.LeituraCacheUsdPorMilhao < 0) { p.LeituraCacheUsdPorMilhao = 0; mudou = true; }
            }
        }

        return mudou;
    }
}
