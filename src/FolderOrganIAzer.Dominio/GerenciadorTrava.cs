using System;
using System.Diagnostics;
using System.Globalization;

namespace FolderOrganIAzer.Dominio;

public class GerenciadorTrava
{
    public bool EhTravaOrfa(Execucao execucao)
    {
        if (execucao.DonoPid == null) return false;

        if (!string.IsNullOrEmpty(execucao.HeartbeatEm))
        {
            try
            {
                var heartbeat = FormatadorData.Ler(execucao.HeartbeatEm);
                if (DateTime.UtcNow - heartbeat > TimeSpan.FromSeconds(60))
                {
                    return true;
                }
            }
            catch (FormatException) { return true; }
        }

        try
        {
            var processo = Process.GetProcessById(execucao.DonoPid.Value);
            var startTimeStr = FormatadorData.Formatar(processo.StartTime);

            if (execucao.DonoInicioProcesso != startTimeStr)
            {
                if (!string.IsNullOrEmpty(execucao.DonoInicioProcesso))
                {
                    try
                    {
                        var dtGravado = FormatadorData.Ler(execucao.DonoInicioProcesso);
                        if (Math.Abs((dtGravado - processo.StartTime.ToUniversalTime()).TotalSeconds) > 1)
                        {
                            return true;
                        }
                    }
                    catch (FormatException) { return true; }
                }
                else
                {
                    return true;
                }
            }
        }
        catch (ArgumentException) { return true; }
        catch (InvalidOperationException) { return true; }

        return false;
    }

    public void Assumir(Execucao execucao)
    {
        var processo = Process.GetCurrentProcess();
        execucao.DonoPid = processo.Id;
        execucao.DonoInicioProcesso = FormatadorData.Formatar(processo.StartTime);
        execucao.HeartbeatEm = FormatadorData.Formatar(DateTime.UtcNow);
        execucao.AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow);
    }

    public void PulsarHeartbeat(Execucao execucao)
    {
        execucao.HeartbeatEm = FormatadorData.Formatar(DateTime.UtcNow);
        execucao.AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow);
    }

    public void Zerar(Execucao execucao)
    {
        execucao.DonoPid = null;
        execucao.DonoInicioProcesso = null;
        execucao.HeartbeatEm = null;
        execucao.AtualizadaEm = FormatadorData.Formatar(DateTime.UtcNow);
    }
}
