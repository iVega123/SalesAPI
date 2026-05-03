using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/vendas-pdv")]
public class VendasPdvController : ControllerBase
{
    private readonly IPdvService _pdv;

    public VendasPdvController(IPdvService pdv) => _pdv = pdv;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendaPdvResponse>>> Listar([FromQuery] int? caixaId, CancellationToken ct)
        => Ok(await _pdv.ListarVendasAsync(caixaId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendaPdvResponse>> Obter(int id, CancellationToken ct)
    {
        var venda = await _pdv.ObterVendaAsync(id, ct);
        if (venda is null) return NotFound();
        return Ok(venda);
    }

    [HttpPost]
    public async Task<ActionResult<VendaPdvResponse>> Finalizar([FromBody] VendaPdvRequest req, CancellationToken ct)
    {
        try
        {
            var venda = await _pdv.FinalizarVendaAsync(req, ct);
            return CreatedAtAction(nameof(Obter), new { id = venda.Id }, venda);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<VendaPdvResponse>> Cancelar(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _pdv.CancelarVendaAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
