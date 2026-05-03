using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CaixaController : ControllerBase
{
    private readonly IPdvService _pdv;

    public CaixaController(IPdvService pdv) => _pdv = pdv;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CaixaResponse>>> Listar(CancellationToken ct)
        => Ok(await _pdv.ListarCaixasAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CaixaResponse>> Obter(int id, CancellationToken ct)
    {
        var caixa = await _pdv.ObterCaixaAsync(id, ct);
        if (caixa is null) return NotFound();
        return Ok(caixa);
    }

    [HttpPost("abrir")]
    public async Task<ActionResult<CaixaResponse>> Abrir([FromBody] AbrirCaixaRequest req, CancellationToken ct)
    {
        try
        {
            var caixa = await _pdv.AbrirCaixaAsync(req, ct);
            return CreatedAtAction(nameof(Obter), new { id = caixa.Id }, caixa);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/fechar")]
    public async Task<ActionResult<CaixaResponse>> Fechar(int id, [FromBody] FecharCaixaRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _pdv.FecharCaixaAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/sangria")]
    public async Task<ActionResult<MovimentoCaixaResponse>> Sangria(int id, [FromBody] MovimentoCaixaRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _pdv.SangriaAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/suprimento")]
    public async Task<ActionResult<MovimentoCaixaResponse>> Suprimento(int id, [FromBody] MovimentoCaixaRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _pdv.SuprimentoAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("{id:int}/movimentos")]
    public async Task<ActionResult<IEnumerable<MovimentoCaixaResponse>>> ListarMovimentos(int id, CancellationToken ct)
        => Ok(await _pdv.ListarMovimentosAsync(id, ct));
}
