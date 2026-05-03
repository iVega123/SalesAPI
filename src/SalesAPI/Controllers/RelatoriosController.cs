using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorio;

    public RelatoriosController(IRelatorioService relatorio) => _relatorio = relatorio;

    // ── Vendas ────────────────────────────────────────────────────────────────

    [HttpGet("vendas")]
    public async Task<ActionResult<RelatorioVendasResponse>> VendasPorPeriodo(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try
        {
            if (fim < inicio) return BadRequest(new { erro = "Data fim deve ser maior que data início." });
            return Ok(await _relatorio.VendasPorPeriodoAsync(inicio, fim, ct));
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("vendas/por-produto")]
    public async Task<ActionResult<List<VendasPorProdutoItem>>> VendasPorProduto(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try { return Ok(await _relatorio.VendasPorProdutoAsync(inicio, fim, ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("vendas/por-vendedor")]
    public async Task<ActionResult<List<VendasPorVendedorItem>>> VendasPorVendedor(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try { return Ok(await _relatorio.VendasPorVendedorAsync(inicio, fim, ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("vendas/por-cliente")]
    public async Task<ActionResult<List<VendasPorClienteItem>>> VendasPorCliente(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try { return Ok(await _relatorio.VendasPorClienteAsync(inicio, fim, ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    // ── Estoque ───────────────────────────────────────────────────────────────

    [HttpGet("estoque")]
    public async Task<ActionResult<List<EstoqueAtualItem>>> EstoqueAtual(CancellationToken ct)
    {
        try { return Ok(await _relatorio.EstoqueAtualAsync(ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("estoque/giro")]
    public async Task<ActionResult<List<GiroEstoqueItem>>> GiroEstoque(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try { return Ok(await _relatorio.GiroEstoqueAsync(inicio, fim, ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("estoque/curva-abc")]
    public async Task<ActionResult<List<CurvaAbcItem>>> CurvaAbc(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try { return Ok(await _relatorio.CurvaAbcAsync(inicio, fim, ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    // ── Exportação Excel ──────────────────────────────────────────────────────

    [HttpGet("vendas/excel")]
    public async Task<IActionResult> ExportarVendasExcel(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try
        {
            var bytes = await _relatorio.ExportarVendasExcelAsync(inicio, fim, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"vendas_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.xlsx");
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("estoque/excel")]
    public async Task<IActionResult> ExportarEstoqueExcel(CancellationToken ct)
    {
        try
        {
            var bytes = await _relatorio.ExportarEstoqueExcelAsync(ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"estoque_{DateTime.Today:yyyyMMdd}.xlsx");
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("estoque/curva-abc/excel")]
    public async Task<IActionResult> ExportarCurvaAbcExcel(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try
        {
            var bytes = await _relatorio.ExportarCurvaAbcExcelAsync(inicio, fim, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"curva_abc_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.xlsx");
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    // ── Exportação PDF ────────────────────────────────────────────────────────

    [HttpGet("vendas/pdf")]
    public async Task<IActionResult> ExportarVendasPdf(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try
        {
            var bytes = await _relatorio.ExportarVendasPdfAsync(inicio, fim, ct);
            return File(bytes, "application/pdf", $"vendas_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.pdf");
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("estoque/pdf")]
    public async Task<IActionResult> ExportarEstoquePdf(CancellationToken ct)
    {
        try
        {
            var bytes = await _relatorio.ExportarEstoquePdfAsync(ct);
            return File(bytes, "application/pdf", $"estoque_{DateTime.Today:yyyyMMdd}.pdf");
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
