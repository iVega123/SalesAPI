using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _service;

    public ComprasController(ICompraService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompraResponse>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompraResponse>> Obter(int id, CancellationToken ct)
    {
        var compra = await _service.ObterAsync(id, ct);
        if (compra is null) return NotFound();
        return Ok(compra);
    }

    [HttpPost]
    public async Task<ActionResult<CompraResponse>> Criar([FromBody] CompraRequest req, CancellationToken ct)
    {
        try
        {
            var compra = await _service.CriarAsync(req, ct);
            return CreatedAtAction(nameof(Obter), new { id = compra.Id }, compra);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/receber")]
    public async Task<ActionResult<CompraResponse>> Receber(int id, [FromBody] ReceberCompraRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ReceberAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/parcelas")]
    public async Task<ActionResult<CompraResponse>> GerarParcelas(int id, [FromBody] GerarParcelasCompraRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GerarParcelasAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<CompraResponse>> Cancelar(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CancelarAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
