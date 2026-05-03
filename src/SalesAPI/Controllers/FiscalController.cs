using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FiscalController : ControllerBase
{
    private readonly IFiscalService _fiscal;
    private readonly IAppDbContext _db;

    public FiscalController(IFiscalService fiscal, IAppDbContext db)
    {
        _fiscal = fiscal;
        _db = db;
    }

    // ── NF-e / NFC-e ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<List<NotaFiscalResponse>>> Listar(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        try { return Ok(await _fiscal.ListarNotasAsync(inicio, fim, ct)); }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NotaFiscalResponse>> Obter(int id, CancellationToken ct)
    {
        var nota = await _fiscal.ObterNotaAsync(id, ct);
        return nota is null ? NotFound() : Ok(nota);
    }

    [HttpPost("emitir")]
    public async Task<ActionResult<NotaFiscalResponse>> Emitir(
        [FromBody] EmitirNfeRequest request,
        CancellationToken ct)
    {
        try
        {
            var nota = await _fiscal.EmitirNfeAsync(request, ct);
            return CreatedAtAction(nameof(Obter), new { id = nota.Id }, nota);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { erro = ex.Message }); }
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<NotaFiscalResponse>> Cancelar(
        int id,
        [FromBody] CancelarNfeRequest request,
        CancellationToken ct)
    {
        try { return Ok(await _fiscal.CancelarNfeAsync(id, request, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("{id:int}/xml")]
    public async Task<IActionResult> DownloadXml(int id, CancellationToken ct)
    {
        try
        {
            var xml = await _fiscal.GerarXmlNfeAsync(id, ct);
            var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
            return File(bytes, "application/xml", $"nfe_{id}.xml");
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    // ── Cálculo de Impostos ───────────────────────────────────────────────────

    [HttpPost("calcular-impostos")]
    public async Task<ActionResult<CalcularImpostosResponse>> CalcularImpostos(
        [FromBody] CalcularImpostosRequest request,
        CancellationToken ct)
    {
        try
        {
            var produto = await _db.Produtos.FindAsync([request.ProdutoId], ct)
                ?? throw new InvalidOperationException($"Produto {request.ProdutoId} não encontrado.");

            return Ok(_fiscal.CalcularImpostos(request, produto));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    // ── SPED Fiscal ───────────────────────────────────────────────────────────

    [HttpGet("sped/{ano:int}/{mes:int}")]
    public async Task<IActionResult> DownloadSped(int ano, int mes, CancellationToken ct)
    {
        try
        {
            if (mes < 1 || mes > 12) return BadRequest(new { erro = "Mês inválido." });

            var bytes = await _fiscal.GerarSpedFiscalAsync(ano, mes, ct);
            return File(bytes, "text/plain", $"SPED_{ano}{mes:D2}.txt");
        }
        catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
