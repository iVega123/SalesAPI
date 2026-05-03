using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public class AlertaEstoqueJob
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertaEstoqueJob> _logger;

    public AlertaEstoqueJob(IAppDbContext db, IEmailService email, IConfiguration config, ILogger<AlertaEstoqueJob> logger)
    {
        _db = db;
        _email = email;
        _config = config;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecutarAsync()
    {
        var emailDestino = _config["Automacao:AlertaEstoque:Email"];
        if (string.IsNullOrWhiteSpace(emailDestino))
        {
            _logger.LogInformation("Alerta de estoque: email não configurado, pulando.");
            return;
        }

        var abaixoMinimo = await _db.Estoques
            .Include(e => e.Produto)
            .AsNoTracking()
            .Where(e => e.Produto!.Status == StatusProduto.Ativo && e.Quantidade < e.Produto.EstoqueMinimo)
            .OrderBy(e => e.Produto!.Nome)
            .ToListAsync();

        if (abaixoMinimo.Count == 0)
        {
            _logger.LogInformation("Alerta de estoque: nenhum produto abaixo do mínimo.");
            return;
        }

        var linhas = abaixoMinimo.Select(e =>
            $"<tr>" +
            $"<td>{e.Produto!.Nome}</td>" +
            $"<td style='text-align:center'>{e.Quantidade}</td>" +
            $"<td style='text-align:center'>{e.Produto.EstoqueMinimo}</td>" +
            $"<td style='text-align:center;color:red'>{e.Produto.EstoqueMinimo - e.Quantidade}</td>" +
            $"</tr>");

        var corpo = $"""
            <html><body>
            <h2>⚠️ Alerta de Estoque Mínimo</h2>
            <p>{abaixoMinimo.Count} produto(s) abaixo do estoque mínimo em {DateTime.Now:dd/MM/yyyy HH:mm}:</p>
            <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;font-family:sans-serif'>
              <thead style='background:#c00;color:white'>
                <tr><th>Produto</th><th>Qtd. Atual</th><th>Mínimo</th><th>Diferença</th></tr>
              </thead>
              <tbody>{string.Join("", linhas)}</tbody>
            </table>
            <p style='color:gray;font-size:12px'>SalesAPI — Gerado automaticamente</p>
            </body></html>
            """;

        await _email.EnviarAsync(emailDestino, $"[SalesAPI] Alerta: {abaixoMinimo.Count} produto(s) abaixo do mínimo", corpo);
        _logger.LogInformation("Alerta de estoque enviado para {Email}: {Qtd} produtos", emailDestino, abaixoMinimo.Count);
    }
}

public class AtualizarContasVencidasJob
{
    private readonly IAppDbContext _db;
    private readonly ILogger<AtualizarContasVencidasJob> _logger;

    public AtualizarContasVencidasJob(IAppDbContext db, ILogger<AtualizarContasVencidasJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecutarAsync()
    {
        var hoje = DateTime.UtcNow.Date;

        var contasPagarVencidas = await _db.ContasPagar
            .Where(c => c.Status == StatusContaPagar.Pendente && c.Vencimento.Date < hoje)
            .ToListAsync();

        foreach (var conta in contasPagarVencidas)
            conta.Status = StatusContaPagar.Vencida;

        var contasReceberVencidas = await _db.ContasReceber
            .Where(c => c.Status == StatusContaReceber.Pendente && c.Vencimento.Date < hoje)
            .ToListAsync();

        foreach (var conta in contasReceberVencidas)
            conta.Status = StatusContaReceber.Vencida;

        var parcelasVencidas = await _db.ParcelasVenda
            .Where(p => p.Status == StatusParcela.Pendente && p.Vencimento.Date < hoje)
            .ToListAsync();

        foreach (var parcela in parcelasVencidas)
            parcela.Status = StatusParcela.Vencida;

        if (contasPagarVencidas.Count + contasReceberVencidas.Count + parcelasVencidas.Count > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Contas vencidas: {Pagar} a pagar, {Receber} a receber, {Parcelas} parcelas",
                contasPagarVencidas.Count, contasReceberVencidas.Count, parcelasVencidas.Count);
        }
    }
}

public class RelatorioSemanalJob
{
    private readonly IRelatorioService _relatorio;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<RelatorioSemanalJob> _logger;

    public RelatorioSemanalJob(IRelatorioService relatorio, IEmailService email, IConfiguration config, ILogger<RelatorioSemanalJob> logger)
    {
        _relatorio = relatorio;
        _email = email;
        _config = config;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecutarAsync()
    {
        var emailDestino = _config["Automacao:RelatorioSemanal:Email"];
        if (string.IsNullOrWhiteSpace(emailDestino))
        {
            _logger.LogInformation("Relatório semanal: email não configurado, pulando.");
            return;
        }

        var fim = DateTime.UtcNow.Date;
        var inicio = fim.AddDays(-7);

        var resumo = await _relatorio.VendasPorPeriodoAsync(inicio, fim);
        var excel = await _relatorio.ExportarVendasExcelAsync(inicio, fim);

        var corpo = $"""
            <html><body>
            <h2>📊 Relatório Semanal de Vendas</h2>
            <p>Período: <strong>{inicio:dd/MM/yyyy}</strong> a <strong>{fim:dd/MM/yyyy}</strong></p>
            <table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse;font-family:sans-serif'>
              <tr><td><strong>Total de Vendas</strong></td><td>{resumo.TotalVendas}</td></tr>
              <tr><td><strong>Receita Bruta</strong></td><td>R$ {resumo.ReceitaBruta:N2}</td></tr>
              <tr><td><strong>Descontos</strong></td><td>R$ {resumo.TotalDescontos:N2}</td></tr>
              <tr><td><strong>Receita Líquida</strong></td><td>R$ {resumo.ReceitaLiquida:N2}</td></tr>
              <tr><td><strong>Total de Itens Vendidos</strong></td><td>{resumo.TotalItens}</td></tr>
            </table>
            <p>Relatório detalhado em anexo (Excel).</p>
            <p style='color:gray;font-size:12px'>SalesAPI — Gerado automaticamente toda segunda-feira</p>
            </body></html>
            """;

        await _email.EnviarComAnexoAsync(
            emailDestino,
            $"[SalesAPI] Relatório Semanal {inicio:dd/MM} a {fim:dd/MM/yyyy}",
            corpo,
            excel,
            $"vendas_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        _logger.LogInformation("Relatório semanal enviado para {Email}", emailDestino);
    }
}

public class BackupDadosJob
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<BackupDadosJob> _logger;

    public BackupDadosJob(IAppDbContext db, IConfiguration config, ILogger<BackupDadosJob> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecutarAsync()
    {
        var pastaBackup = _config["Automacao:Backup:Pasta"] ?? "backups";
        Directory.CreateDirectory(pastaBackup);

        var arquivo = Path.Combine(pastaBackup, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");

        var totalVendas = await _db.Vendas.CountAsync();
        var totalProdutos = await _db.Produtos.CountAsync();
        var totalClientes = await _db.Clientes.CountAsync();
        var totalCompras = await _db.Compras.CountAsync();

        var resumo = $"""
            BACKUP SALESAPI — {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}
            =====================================
            Vendas:    {totalVendas}
            Produtos:  {totalProdutos}
            Clientes:  {totalClientes}
            Compras:   {totalCompras}
            =====================================
            Banco: PostgreSQL (ver detalhes na connection string)
            Status: Para backup completo, use pg_dump na conexão configurada.
            """;

        await File.WriteAllTextAsync(arquivo, resumo);

        foreach (var antigo in Directory.GetFiles(pastaBackup, "backup_*.txt")
            .Where(f => File.GetCreationTime(f) < DateTime.Now.AddDays(-30)))
        {
            File.Delete(antigo);
        }

        _logger.LogInformation("Backup registrado em {Arquivo}", arquivo);
    }
}

public static class HangfireJobsRegistration
{
    public static void RegistrarJobs()
    {
        RecurringJob.AddOrUpdate<AtualizarContasVencidasJob>(
            "atualizar-contas-vencidas",
            j => j.ExecutarAsync(),
            "0 6 * * *");

        RecurringJob.AddOrUpdate<AlertaEstoqueJob>(
            "alerta-estoque-minimo",
            j => j.ExecutarAsync(),
            "0 7 * * *");

        RecurringJob.AddOrUpdate<RelatorioSemanalJob>(
            "relatorio-semanal",
            j => j.ExecutarAsync(),
            "0 8 * * 1");

        RecurringJob.AddOrUpdate<BackupDadosJob>(
            "backup-diario",
            j => j.ExecutarAsync(),
            "0 0 * * *");
    }
}
