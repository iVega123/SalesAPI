using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface IRelatorioService
{
    // Vendas
    Task<RelatorioVendasResponse> VendasPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<List<VendasPorProdutoItem>> VendasPorProdutoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<List<VendasPorVendedorItem>> VendasPorVendedorAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<List<VendasPorClienteItem>> VendasPorClienteAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);

    // Estoque
    Task<List<EstoqueAtualItem>> EstoqueAtualAsync(CancellationToken ct = default);
    Task<List<GiroEstoqueItem>> GiroEstoqueAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<List<CurvaAbcItem>> CurvaAbcAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);

    // Exportação
    Task<byte[]> ExportarVendasExcelAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<byte[]> ExportarVendasPdfAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<byte[]> ExportarEstoqueExcelAsync(CancellationToken ct = default);
    Task<byte[]> ExportarEstoquePdfAsync(CancellationToken ct = default);
    Task<byte[]> ExportarCurvaAbcExcelAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
}

public class RelatorioService : IRelatorioService
{
    private readonly IAppDbContext _db;

    public RelatorioService(IAppDbContext db) => _db = db;

    // ── Vendas por Período ────────────────────────────────────────────────────

    public async Task<RelatorioVendasResponse> VendasPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var fimDia = fim.Date.AddDays(1).AddTicks(-1);

        var vendas = await _db.Vendas
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Confirmada && v.DataVenda >= inicio && v.DataVenda <= fimDia)
            .ToListAsync(ct);

        var vendasPdv = await _db.VendasPdv
            .AsNoTracking()
            .Where(v => v.Status == StatusVendaPdv.Finalizada && v.DataVenda >= inicio && v.DataVenda <= fimDia)
            .ToListAsync(ct);

        var receitaBruta = vendas.Sum(v => v.Total + v.Desconto) + vendasPdv.Sum(v => v.Total + v.Desconto);
        var descontos = vendas.Sum(v => v.Desconto) + vendasPdv.Sum(v => v.Desconto);

        var totalItens = await _db.ItensVenda
            .AsNoTracking()
            .Where(i => i.Venda != null && i.Venda.Status == StatusVenda.Confirmada
                     && i.Venda.DataVenda >= inicio && i.Venda.DataVenda <= fimDia)
            .SumAsync(i => i.Quantidade, ct);

        var totalItensPdv = await _db.ItensPdv
            .AsNoTracking()
            .Where(i => i.VendaPdv != null && i.VendaPdv.Status == StatusVendaPdv.Finalizada
                     && i.VendaPdv.DataVenda >= inicio && i.VendaPdv.DataVenda <= fimDia)
            .SumAsync(i => i.Quantidade, ct);

        var porDia = new List<VendasDiaItem>();
        for (var d = inicio.Date; d <= fim.Date; d = d.AddDays(1))
        {
            var dFim = d.AddDays(1).AddTicks(-1);
            var qtd = vendas.Count(v => v.DataVenda >= d && v.DataVenda <= dFim)
                    + vendasPdv.Count(v => v.DataVenda >= d && v.DataVenda <= dFim);
            var total = vendas.Where(v => v.DataVenda >= d && v.DataVenda <= dFim).Sum(v => v.Total)
                      + vendasPdv.Where(v => v.DataVenda >= d && v.DataVenda <= dFim).Sum(v => v.Total);
            if (qtd > 0)
                porDia.Add(new VendasDiaItem(d, qtd, total));
        }

        return new RelatorioVendasResponse(
            inicio.Date,
            fim.Date,
            vendas.Count + vendasPdv.Count,
            receitaBruta,
            descontos,
            receitaBruta - descontos,
            totalItens + totalItensPdv,
            porDia);
    }

    // ── Vendas por Produto ────────────────────────────────────────────────────

    public async Task<List<VendasPorProdutoItem>> VendasPorProdutoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var fimDia = fim.Date.AddDays(1).AddTicks(-1);

        var itensVenda = await _db.ItensVenda
            .Include(i => i.Produto)
            .Include(i => i.Venda)
            .AsNoTracking()
            .Where(i => i.Venda!.Status == StatusVenda.Confirmada
                     && i.Venda.DataVenda >= inicio && i.Venda.DataVenda <= fimDia)
            .ToListAsync(ct);

        var itensPdv = await _db.ItensPdv
            .Include(i => i.Produto)
            .Include(i => i.VendaPdv)
            .AsNoTracking()
            .Where(i => i.VendaPdv!.Status == StatusVendaPdv.Finalizada
                     && i.VendaPdv.DataVenda >= inicio && i.VendaPdv.DataVenda <= fimDia)
            .ToListAsync(ct);

        var porProduto = itensVenda
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "Desconhecido", Custo = i.Produto?.PrecoCusto ?? 0 })
            .Select(g => new
            {
                g.Key.ProdutoId,
                g.Key.Nome,
                g.Key.Custo,
                Quantidade = g.Sum(i => i.Quantidade),
                Receita = g.Sum(i => i.PrecoUnitario * i.Quantidade)
            })
            .ToList();

        var porProdutoPdv = itensPdv
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "Desconhecido", Custo = i.Produto?.PrecoCusto ?? 0 })
            .Select(g => new
            {
                g.Key.ProdutoId,
                g.Key.Nome,
                g.Key.Custo,
                Quantidade = g.Sum(i => i.Quantidade),
                Receita = g.Sum(i => (i.PrecoUnitario * i.Quantidade) - i.Desconto)
            })
            .ToList();

        return porProduto
            .Concat(porProdutoPdv)
            .GroupBy(x => new { x.ProdutoId, x.Nome, x.Custo })
            .Select(g => new VendasPorProdutoItem(
                g.Key.ProdutoId,
                g.Key.Nome,
                g.Sum(x => x.Quantidade),
                g.Sum(x => x.Receita),
                g.Key.Custo * g.Sum(x => x.Quantidade),
                g.Sum(x => x.Receita) - (g.Key.Custo * g.Sum(x => x.Quantidade))))
            .OrderByDescending(x => x.ReceitaTotal)
            .ToList();
    }

    // ── Vendas por Vendedor ───────────────────────────────────────────────────

    public async Task<List<VendasPorVendedorItem>> VendasPorVendedorAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var fimDia = fim.Date.AddDays(1).AddTicks(-1);

        var vendas = await _db.Vendas
            .Include(v => v.Vendedor)
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Confirmada
                     && v.VendedorId != null
                     && v.DataVenda >= inicio && v.DataVenda <= fimDia)
            .ToListAsync(ct);

        var comissoes = await _db.Comissoes
            .AsNoTracking()
            .Where(c => c.Venda!.DataVenda >= inicio && c.Venda.DataVenda <= fimDia)
            .GroupBy(c => c.VendedorId)
            .Select(g => new { VendedorId = g.Key, TotalComissoes = g.Sum(c => c.ValorComissao) })
            .ToListAsync(ct);

        return vendas
            .GroupBy(v => new { v.VendedorId, Nome = v.Vendedor?.Nome ?? "Sem vendedor" })
            .Select(g => new VendasPorVendedorItem(
                g.Key.VendedorId!.Value,
                g.Key.Nome,
                g.Count(),
                g.Sum(v => v.Total),
                comissoes.FirstOrDefault(c => c.VendedorId == g.Key.VendedorId)?.TotalComissoes ?? 0))
            .OrderByDescending(x => x.ReceitaTotal)
            .ToList();
    }

    // ── Vendas por Cliente ────────────────────────────────────────────────────

    public async Task<List<VendasPorClienteItem>> VendasPorClienteAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var fimDia = fim.Date.AddDays(1).AddTicks(-1);

        var vendas = await _db.Vendas
            .Include(v => v.Cliente)
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Confirmada && v.DataVenda >= inicio && v.DataVenda <= fimDia)
            .ToListAsync(ct);

        var vendasPdv = await _db.VendasPdv
            .Include(v => v.Cliente)
            .AsNoTracking()
            .Where(v => v.Status == StatusVendaPdv.Finalizada
                     && v.ClienteId != null
                     && v.DataVenda >= inicio && v.DataVenda <= fimDia)
            .ToListAsync(ct);

        var porCliente = vendas
            .Where(v => v.Cliente != null)
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente!.Nome })
            .Select(g => new
            {
                g.Key.ClienteId,
                g.Key.Nome,
                Total = g.Sum(v => v.Total),
                Qtd = g.Count(),
                Ultima = g.Max(v => v.DataVenda)
            })
            .ToList();

        var porClientePdv = vendasPdv
            .Where(v => v.Cliente != null)
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente!.Nome })
            .Select(g => new
            {
                ClienteId = g.Key.ClienteId!.Value,
                g.Key.Nome,
                Total = g.Sum(v => v.Total),
                Qtd = g.Count(),
                Ultima = g.Max(v => v.DataVenda)
            })
            .ToList();

        return porCliente
            .Concat(porClientePdv.Select(x => new { x.ClienteId, x.Nome, x.Total, x.Qtd, x.Ultima }))
            .GroupBy(x => new { x.ClienteId, x.Nome })
            .Select(g => new VendasPorClienteItem(
                g.Key.ClienteId,
                g.Key.Nome,
                g.Sum(x => x.Qtd),
                g.Sum(x => x.Total),
                g.Max(x => x.Ultima)))
            .OrderByDescending(x => x.TotalGasto)
            .ToList();
    }

    // ── Estoque Atual ─────────────────────────────────────────────────────────

    public async Task<List<EstoqueAtualItem>> EstoqueAtualAsync(CancellationToken ct = default)
    {
        var estoques = await _db.Estoques
            .Include(e => e.Produto!).ThenInclude(p => p!.Categoria)
            .AsNoTracking()
            .Where(e => e.Produto!.Status == StatusProduto.Ativo)
            .ToListAsync(ct);

        return estoques
            .Select(e => new EstoqueAtualItem(
                e.ProdutoId,
                e.Produto!.Nome,
                e.Produto.SKU,
                e.Produto.Categoria?.Nome,
                e.Quantidade,
                e.Produto.EstoqueMinimo,
                e.Quantidade < e.Produto.EstoqueMinimo,
                e.Produto.PrecoCusto,
                e.Produto.PrecoVenda,
                e.Produto.PrecoCusto * e.Quantidade))
            .OrderBy(x => x.ProdutoNome)
            .ToList();
    }

    // ── Giro de Estoque ───────────────────────────────────────────────────────

    public async Task<List<GiroEstoqueItem>> GiroEstoqueAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var fimDia = fim.Date.AddDays(1).AddTicks(-1);
        var dias = (fim.Date - inicio.Date).TotalDays + 1;

        var movimentacoes = await _db.MovimentacoesEstoque
            .Include(m => m.Produto)
            .AsNoTracking()
            .Where(m => m.CriadoEm >= inicio && m.CriadoEm <= fimDia)
            .ToListAsync(ct);

        var estoqueAtual = await _db.Estoques
            .AsNoTracking()
            .ToDictionaryAsync(e => e.ProdutoId, e => e.Quantidade, ct);

        return movimentacoes
            .GroupBy(m => new { m.ProdutoId, Nome = m.Produto?.Nome ?? "Desconhecido" })
            .Select(g => new GiroEstoqueItem(
                g.Key.ProdutoId,
                g.Key.Nome,
                g.Where(m => m.Tipo == TipoMovimentacao.Entrada).Sum(m => m.Quantidade),
                g.Where(m => m.Tipo == TipoMovimentacao.Saida || m.Tipo == TipoMovimentacao.Perda).Sum(m => m.Quantidade),
                estoqueAtual.GetValueOrDefault(g.Key.ProdutoId),
                dias > 0 ? Math.Round(g.Where(m => m.Tipo == TipoMovimentacao.Saida).Sum(m => (decimal)m.Quantidade) / (decimal)dias, 2) : 0))
            .OrderByDescending(x => x.GiroMedio)
            .ToList();
    }

    // ── Curva ABC ─────────────────────────────────────────────────────────────

    public async Task<List<CurvaAbcItem>> CurvaAbcAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var porProduto = await VendasPorProdutoAsync(inicio, fim, ct);
        var totalReceita = porProduto.Sum(p => p.ReceitaTotal);
        if (totalReceita == 0) return [];

        var acumulado = 0m;
        return porProduto
            .OrderByDescending(p => p.ReceitaTotal)
            .Select((p, i) =>
            {
                var pct = totalReceita > 0 ? p.ReceitaTotal / totalReceita * 100 : 0;
                acumulado += pct;
                var classe = acumulado <= 80 ? ClassificacaoAbc.A
                           : acumulado <= 95 ? ClassificacaoAbc.B
                           : ClassificacaoAbc.C;
                return new CurvaAbcItem(i + 1, p.ProdutoId, p.ProdutoNome, p.ReceitaTotal,
                    Math.Round(pct, 2), Math.Round(acumulado, 2), classe);
            })
            .ToList();
    }

    // ── Exportação Excel ──────────────────────────────────────────────────────

    public async Task<byte[]> ExportarVendasExcelAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var resumo = await VendasPorPeriodoAsync(inicio, fim, ct);
        var porProduto = await VendasPorProdutoAsync(inicio, fim, ct);
        var porCliente = await VendasPorClienteAsync(inicio, fim, ct);

        using var wb = new XLWorkbook();

        var wsResumo = wb.Worksheets.Add("Resumo");
        wsResumo.Cell(1, 1).Value = "Relatório de Vendas";
        wsResumo.Cell(1, 1).Style.Font.Bold = true;
        wsResumo.Cell(2, 1).Value = "Período:";
        wsResumo.Cell(2, 2).Value = $"{inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
        wsResumo.Cell(3, 1).Value = "Total de Vendas:";
        wsResumo.Cell(3, 2).Value = resumo.TotalVendas;
        wsResumo.Cell(4, 1).Value = "Receita Bruta:";
        wsResumo.Cell(4, 2).Value = resumo.ReceitaBruta;
        wsResumo.Cell(4, 2).Style.NumberFormat.Format = "R$ #,##0.00";
        wsResumo.Cell(5, 1).Value = "Descontos:";
        wsResumo.Cell(5, 2).Value = resumo.TotalDescontos;
        wsResumo.Cell(5, 2).Style.NumberFormat.Format = "R$ #,##0.00";
        wsResumo.Cell(6, 1).Value = "Receita Líquida:";
        wsResumo.Cell(6, 2).Value = resumo.ReceitaLiquida;
        wsResumo.Cell(6, 2).Style.NumberFormat.Format = "R$ #,##0.00";
        wsResumo.Columns().AdjustToContents();

        var wsDia = wb.Worksheets.Add("Vendas por Dia");
        wsDia.Cell(1, 1).Value = "Data";
        wsDia.Cell(1, 2).Value = "Qtd. Vendas";
        wsDia.Cell(1, 3).Value = "Total";
        EstiloCabecalho(wsDia, 1, 3);
        for (var i = 0; i < resumo.PorDia.Count; i++)
        {
            var d = resumo.PorDia[i];
            wsDia.Cell(i + 2, 1).Value = d.Data.ToString("dd/MM/yyyy");
            wsDia.Cell(i + 2, 2).Value = d.Vendas;
            wsDia.Cell(i + 2, 3).Value = d.Total;
            wsDia.Cell(i + 2, 3).Style.NumberFormat.Format = "R$ #,##0.00";
        }
        wsDia.Columns().AdjustToContents();

        var wsProduto = wb.Worksheets.Add("Por Produto");
        wsProduto.Cell(1, 1).Value = "Produto";
        wsProduto.Cell(1, 2).Value = "Qtd. Vendida";
        wsProduto.Cell(1, 3).Value = "Receita";
        wsProduto.Cell(1, 4).Value = "Custo";
        wsProduto.Cell(1, 5).Value = "Lucro Bruto";
        EstiloCabecalho(wsProduto, 1, 5);
        for (var i = 0; i < porProduto.Count; i++)
        {
            var p = porProduto[i];
            wsProduto.Cell(i + 2, 1).Value = p.ProdutoNome;
            wsProduto.Cell(i + 2, 2).Value = p.QuantidadeVendida;
            wsProduto.Cell(i + 2, 3).Value = p.ReceitaTotal;
            wsProduto.Cell(i + 2, 3).Style.NumberFormat.Format = "R$ #,##0.00";
            wsProduto.Cell(i + 2, 4).Value = p.CustoTotal;
            wsProduto.Cell(i + 2, 4).Style.NumberFormat.Format = "R$ #,##0.00";
            wsProduto.Cell(i + 2, 5).Value = p.LucroBruto;
            wsProduto.Cell(i + 2, 5).Style.NumberFormat.Format = "R$ #,##0.00";
        }
        wsProduto.Columns().AdjustToContents();

        var wsCliente = wb.Worksheets.Add("Por Cliente");
        wsCliente.Cell(1, 1).Value = "Cliente";
        wsCliente.Cell(1, 2).Value = "Qtd. Compras";
        wsCliente.Cell(1, 3).Value = "Total Gasto";
        wsCliente.Cell(1, 4).Value = "Última Compra";
        EstiloCabecalho(wsCliente, 1, 4);
        for (var i = 0; i < porCliente.Count; i++)
        {
            var c = porCliente[i];
            wsCliente.Cell(i + 2, 1).Value = c.ClienteNome;
            wsCliente.Cell(i + 2, 2).Value = c.TotalVendas;
            wsCliente.Cell(i + 2, 3).Value = c.TotalGasto;
            wsCliente.Cell(i + 2, 3).Style.NumberFormat.Format = "R$ #,##0.00";
            wsCliente.Cell(i + 2, 4).Value = c.UltimaCompra.ToString("dd/MM/yyyy");
        }
        wsCliente.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportarEstoqueExcelAsync(CancellationToken ct = default)
    {
        var estoque = await EstoqueAtualAsync(ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Estoque Atual");
        ws.Cell(1, 1).Value = "Produto";
        ws.Cell(1, 2).Value = "SKU";
        ws.Cell(1, 3).Value = "Categoria";
        ws.Cell(1, 4).Value = "Qtd. Atual";
        ws.Cell(1, 5).Value = "Estoque Mín.";
        ws.Cell(1, 6).Value = "Abaixo Mín.";
        ws.Cell(1, 7).Value = "Preço Custo";
        ws.Cell(1, 8).Value = "Preço Venda";
        ws.Cell(1, 9).Value = "Valor em Estoque";
        EstiloCabecalho(ws, 1, 9);

        for (var i = 0; i < estoque.Count; i++)
        {
            var e = estoque[i];
            ws.Cell(i + 2, 1).Value = e.ProdutoNome;
            ws.Cell(i + 2, 2).Value = e.SKU ?? "";
            ws.Cell(i + 2, 3).Value = e.Categoria ?? "";
            ws.Cell(i + 2, 4).Value = e.Quantidade;
            ws.Cell(i + 2, 5).Value = e.EstoqueMinimo;
            ws.Cell(i + 2, 6).Value = e.AbaixoMinimo ? "SIM" : "NÃO";
            if (e.AbaixoMinimo)
                ws.Cell(i + 2, 6).Style.Font.FontColor = XLColor.Red;
            ws.Cell(i + 2, 7).Value = e.PrecoCusto;
            ws.Cell(i + 2, 7).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(i + 2, 8).Value = e.PrecoVenda;
            ws.Cell(i + 2, 8).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(i + 2, 9).Value = e.ValorEstoque;
            ws.Cell(i + 2, 9).Style.NumberFormat.Format = "R$ #,##0.00";
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportarCurvaAbcExcelAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var curva = await CurvaAbcAsync(inicio, fim, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Curva ABC");
        ws.Cell(1, 1).Value = "Posição";
        ws.Cell(1, 2).Value = "Produto";
        ws.Cell(1, 3).Value = "Receita Total";
        ws.Cell(1, 4).Value = "% Receita";
        ws.Cell(1, 5).Value = "% Acumulado";
        ws.Cell(1, 6).Value = "Classe";
        EstiloCabecalho(ws, 1, 6);

        for (var i = 0; i < curva.Count; i++)
        {
            var c = curva[i];
            ws.Cell(i + 2, 1).Value = c.Posicao;
            ws.Cell(i + 2, 2).Value = c.ProdutoNome;
            ws.Cell(i + 2, 3).Value = c.ReceitaTotal;
            ws.Cell(i + 2, 3).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(i + 2, 4).Value = c.PercentualReceita / 100;
            ws.Cell(i + 2, 4).Style.NumberFormat.Format = "0.00%";
            ws.Cell(i + 2, 5).Value = c.PercentualAcumulado / 100;
            ws.Cell(i + 2, 5).Style.NumberFormat.Format = "0.00%";
            ws.Cell(i + 2, 6).Value = c.Classificacao.ToString();
            ws.Cell(i + 2, 6).Style.Font.FontColor = c.Classificacao switch
            {
                ClassificacaoAbc.A => XLColor.DarkGreen,
                ClassificacaoAbc.B => XLColor.DarkOrange,
                _ => XLColor.Red
            };
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Exportação PDF ────────────────────────────────────────────────────────

    public async Task<byte[]> ExportarVendasPdfAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var resumo = await VendasPorPeriodoAsync(inicio, fim, ct);
        var porProduto = await VendasPorProdutoAsync(inicio, fim, ct);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().BorderBottom(1).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Text("Relatório de Vendas").FontSize(16).Bold();
                    row.ConstantItem(200).AlignRight()
                       .Text($"{inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}").FontSize(10);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten3).Padding(8).Row(row =>
                    {
                        col.Item().Text("Resumo").Bold().FontSize(12);
                    });

                    col.Item().PaddingVertical(6).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        TableRow(table, "Total de Vendas", resumo.TotalVendas.ToString());
                        TableRow(table, "Receita Bruta", $"R$ {resumo.ReceitaBruta:N2}");
                        TableRow(table, "Descontos", $"R$ {resumo.TotalDescontos:N2}");
                        TableRow(table, "Receita Líquida", $"R$ {resumo.ReceitaLiquida:N2}");
                        TableRow(table, "Total de Itens", resumo.TotalItens.ToString());
                    });

                    col.Item().PaddingTop(10).Text("Top Produtos por Receita").Bold().FontSize(12);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Darken2).Padding(4)
                             .Text("Produto").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Grey.Darken2).Padding(4)
                             .Text("Qtd").FontColor(Colors.White).Bold().AlignCenter();
                            h.Cell().Background(Colors.Grey.Darken2).Padding(4)
                             .Text("Receita").FontColor(Colors.White).Bold().AlignRight();
                            h.Cell().Background(Colors.Grey.Darken2).Padding(4)
                             .Text("Lucro").FontColor(Colors.White).Bold().AlignRight();
                        });

                        foreach (var (p, idx) in porProduto.Take(20).Select((p, i) => (p, i)))
                        {
                            var bg = idx % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            table.Cell().Background(bg).Padding(4).Text(p.ProdutoNome);
                            table.Cell().Background(bg).Padding(4).Text(p.QuantidadeVendida.ToString()).AlignCenter();
                            table.Cell().Background(bg).Padding(4).Text($"R$ {p.ReceitaTotal:N2}").AlignRight();
                            table.Cell().Background(bg).Padding(4).Text($"R$ {p.LucroBruto:N2}").AlignRight();
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Gerado em ").FontSize(8);
                        t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                        t.Span(" — Página ").FontSize(8);
                        t.CurrentPageNumber().FontSize(8);
                        t.Span(" de ").FontSize(8);
                        t.TotalPages().FontSize(8);
                    });
            });
        });

        return doc.GeneratePdf();
    }

    public async Task<byte[]> ExportarEstoquePdfAsync(CancellationToken ct = default)
    {
        var estoque = await EstoqueAtualAsync(ct);
        var abaixoMinimo = estoque.Where(e => e.AbaixoMinimo).ToList();
        var valorTotal = estoque.Sum(e => e.ValorEstoque);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().BorderBottom(1).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Text("Relatório de Estoque Atual").FontSize(16).Bold();
                    row.ConstantItem(200).AlignRight()
                       .Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().PaddingBottom(6).Row(row =>
                    {
                        row.RelativeItem().Text($"Total de Produtos: {estoque.Count}  |  " +
                            $"Abaixo do Mínimo: {abaixoMinimo.Count}  |  " +
                            $"Valor em Estoque: R$ {valorTotal:N2}").Bold();
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            foreach (var titulo in new[] { "Produto", "SKU", "Qtd.", "Mín.", "Preço Venda", "Valor Est." })
                                h.Cell().Background(Colors.Grey.Darken2).Padding(4)
                                 .Text(titulo).FontColor(Colors.White).Bold();
                        });

                        foreach (var (e, idx) in estoque.Select((e, i) => (e, i)))
                        {
                            var bg = e.AbaixoMinimo ? Colors.Red.Lighten4
                                   : idx % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            table.Cell().Background(bg).Padding(3).Text(e.ProdutoNome);
                            table.Cell().Background(bg).Padding(3).Text(e.SKU ?? "");
                            table.Cell().Background(bg).Padding(3).Text(e.Quantidade.ToString()).AlignCenter();
                            table.Cell().Background(bg).Padding(3).Text(e.EstoqueMinimo.ToString()).AlignCenter();
                            table.Cell().Background(bg).Padding(3).Text($"R$ {e.PrecoVenda:N2}").AlignRight();
                            table.Cell().Background(bg).Padding(3).Text($"R$ {e.ValorEstoque:N2}").AlignRight();
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Página ").FontSize(8);
                        t.CurrentPageNumber().FontSize(8);
                        t.Span(" de ").FontSize(8);
                        t.TotalPages().FontSize(8);
                    });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EstiloCabecalho(IXLWorksheet ws, int linha, int ultimaColuna)
    {
        var range = ws.Range(ws.Cell(linha, 1), ws.Cell(linha, ultimaColuna));
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
        range.Style.Font.FontColor = XLColor.White;
    }

    private static void TableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(4).Text(label).Bold();
        table.Cell().Padding(4).Text(value);
    }
}
