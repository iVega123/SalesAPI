using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AutomacaoController : ControllerBase
{
    // ── Execução Manual dos Jobs ──────────────────────────────────────────────

    [HttpPost("jobs/alerta-estoque")]
    public IActionResult TriggerAlertaEstoque()
    {
        var jobId = BackgroundJob.Enqueue<AlertaEstoqueJob>(j => j.ExecutarAsync());
        return Ok(new { jobId, mensagem = "Job de alerta de estoque enfileirado." });
    }

    [HttpPost("jobs/atualizar-contas-vencidas")]
    public IActionResult TriggerAtualizarContasVencidas()
    {
        var jobId = BackgroundJob.Enqueue<AtualizarContasVencidasJob>(j => j.ExecutarAsync());
        return Ok(new { jobId, mensagem = "Job de atualização de contas vencidas enfileirado." });
    }

    [HttpPost("jobs/relatorio-semanal")]
    public IActionResult TriggerRelatorioSemanal()
    {
        var jobId = BackgroundJob.Enqueue<RelatorioSemanalJob>(j => j.ExecutarAsync());
        return Ok(new { jobId, mensagem = "Job de relatório semanal enfileirado." });
    }

    [HttpPost("jobs/backup")]
    public IActionResult TriggerBackup()
    {
        var jobId = BackgroundJob.Enqueue<BackupDadosJob>(j => j.ExecutarAsync());
        return Ok(new { jobId, mensagem = "Job de backup enfileirado." });
    }

    // ── Informações dos Jobs Recorrentes ──────────────────────────────────────

    [HttpGet("jobs")]
    public IActionResult ListarJobs()
    {
        var jobs = new[]
        {
            new { id = "atualizar-contas-vencidas", descricao = "Atualiza contas vencidas", cron = "0 6 * * *", descricaoCron = "Diariamente às 06:00" },
            new { id = "alerta-estoque-minimo",     descricao = "Alerta de estoque mínimo", cron = "0 7 * * *", descricaoCron = "Diariamente às 07:00" },
            new { id = "relatorio-semanal",          descricao = "Relatório semanal por email", cron = "0 8 * * 1", descricaoCron = "Toda segunda-feira às 08:00" },
            new { id = "backup-diario",              descricao = "Backup diário de dados",  cron = "0 0 * * *", descricaoCron = "Diariamente à meia-noite" }
        };
        return Ok(jobs);
    }
}
