using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendasController : ControllerBase
{
    private readonly IVendaService _service;

    public VendasController(IVendaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendaResponse>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendaResponse>> Obter(int id, CancellationToken ct)
    {
        var venda = await _service.ObterAsync(id, ct);
        if (venda is null) return NotFound();
        return Ok(venda);
    }

    [HttpPost]
    public async Task<ActionResult<VendaResponse>> Criar([FromBody] VendaRequest req, CancellationToken ct)
    {
        try
        {
            var venda = await _service.CriarAsync(req, ct);
            return CreatedAtAction(nameof(Obter), new { id = venda.Id }, venda);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<VendaResponse>> Cancelar(int id, CancellationToken ct)
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

    [HttpGet("{id:int}/parcelas")]
    public async Task<ActionResult<VendaResponse>> ObterParcelas(int id, CancellationToken ct)
    {
        var venda = await _service.ObterAsync(id, ct);
        if (venda is null) return NotFound();
        return Ok(venda.Parcelas);
    }

    [HttpPost("{id:int}/parcelas/{parcelaId:int}/pagar")]
    public async Task<ActionResult<ParcelaVendaResponse>> PagarParcela(int id, int parcelaId, [FromBody] PagarParcelaRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.PagarParcelaAsync(id, parcelaId, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
