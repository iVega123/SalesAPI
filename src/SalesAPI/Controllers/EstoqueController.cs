using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstoqueController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IEstoqueService _service;

    public EstoqueController(IAppDbContext db, IEstoqueService service)
    {
        _db = db;
        _service = service;
    }

    [HttpGet("{produtoId:int}")]
    public async Task<ActionResult<EstoqueResponse>> Obter(int produtoId, CancellationToken ct)
    {
        var estoque = await _db.Estoques
            .Include(s => s.Produto)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProdutoId == produtoId, ct);

        if (estoque is null) return NotFound();

        return Ok(new EstoqueResponse(
            estoque.ProdutoId,
            estoque.Produto?.Nome ?? string.Empty,
            estoque.Quantidade,
            estoque.AtualizadoEm));
    }

    [HttpGet("{produtoId:int}/movimentacoes")]
    public async Task<ActionResult<IEnumerable<MovimentacaoEstoqueResponse>>> Movimentacoes(int produtoId, CancellationToken ct)
        => Ok(await _service.ListarMovimentacoesAsync(produtoId, ct));

    [HttpGet("alertas")]
    public async Task<ActionResult<IEnumerable<AlertaEstoqueResponse>>> Alertas(CancellationToken ct)
        => Ok(await _service.AlertasAbaixoMinimoAsync(ct));

    [HttpPost("entrada")]
    public async Task<ActionResult<MovimentacaoEstoqueResponse>> Entrada([FromBody] EntradaEstoqueRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.EntradaAsync(req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("saida")]
    public async Task<ActionResult<MovimentacaoEstoqueResponse>> Saida([FromBody] SaidaEstoqueRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.SaidaAsync(req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("ajuste")]
    public async Task<ActionResult<MovimentacaoEstoqueResponse>> Ajuste([FromBody] AjusteEstoqueMovRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.AjustarAsync(req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ── Inventários ───────────────────────────────────────────────────────────

    [HttpPost("inventarios")]
    public async Task<ActionResult<InventarioResponse>> IniciarInventario([FromBody] IniciarInventarioRequest req, CancellationToken ct)
    {
        try
        {
            var inventario = await _service.IniciarInventarioAsync(req, ct);
            return CreatedAtAction(nameof(ObterInventario), new { id = inventario.Id }, inventario);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("inventarios/{id:int}")]
    public async Task<ActionResult<InventarioResponse>> ObterInventario(int id, CancellationToken ct)
    {
        var inventario = await _db.Inventarios
            .Include(i => i.Ajustes).ThenInclude(a => a.Produto)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inventario is null) return NotFound();

        return Ok(new InventarioResponse(
            inventario.Id,
            inventario.Descricao,
            inventario.IniciadoEm,
            inventario.FinalizadoEm,
            inventario.Finalizado,
            inventario.Ajustes.Select(a => new AjusteEstoqueResponse(
                a.ProdutoId,
                a.Produto?.Nome ?? string.Empty,
                a.QuantidadeContada,
                a.QuantidadeSistema,
                a.Diferenca,
                a.Motivo)).ToList()));
    }

    [HttpPost("inventarios/{id:int}/ajustes")]
    public async Task<ActionResult<InventarioResponse>> RegistrarAjuste(int id, [FromBody] RegistrarAjusteRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.RegistrarAjusteInventarioAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("inventarios/{id:int}/finalizar")]
    public async Task<ActionResult<InventarioResponse>> FinalizarInventario(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.FinalizarInventarioAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
