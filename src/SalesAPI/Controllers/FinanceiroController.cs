using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/financeiro")]
public class FinanceiroController : ControllerBase
{
    private readonly IFinanceiroService _financeiro;

    public FinanceiroController(IFinanceiroService financeiro) => _financeiro = financeiro;

    // ── Contas Bancárias ──────────────────────────────────────────────────────

    [HttpGet("contas-bancarias")]
    public async Task<ActionResult<IEnumerable<ContaBancariaResponse>>> ListarContasBancarias(CancellationToken ct)
        => Ok(await _financeiro.ListarContasBancariasAsync(ct));

    [HttpGet("contas-bancarias/{id:int}")]
    public async Task<ActionResult<ContaBancariaResponse>> ObterContaBancaria(int id, CancellationToken ct)
    {
        var conta = await _financeiro.ObterContaBancariaAsync(id, ct);
        if (conta is null) return NotFound();
        return Ok(conta);
    }

    [HttpPost("contas-bancarias")]
    public async Task<ActionResult<ContaBancariaResponse>> CriarContaBancaria([FromBody] ContaBancariaRequest req, CancellationToken ct)
    {
        try
        {
            var conta = await _financeiro.CriarContaBancariaAsync(req, ct);
            return CreatedAtAction(nameof(ObterContaBancaria), new { id = conta.Id }, conta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ── Contas a Receber ──────────────────────────────────────────────────────

    [HttpGet("contas-receber")]
    public async Task<ActionResult<IEnumerable<ContaReceberResponse>>> ListarContasReceber(CancellationToken ct)
        => Ok(await _financeiro.ListarContasReceberAsync(ct));

    [HttpGet("contas-receber/{id:int}")]
    public async Task<ActionResult<ContaReceberResponse>> ObterContaReceber(int id, CancellationToken ct)
    {
        var conta = await _financeiro.ObterContaReceberAsync(id, ct);
        if (conta is null) return NotFound();
        return Ok(conta);
    }

    [HttpPost("contas-receber")]
    public async Task<ActionResult<ContaReceberResponse>> CriarContaReceber([FromBody] ContaReceberRequest req, CancellationToken ct)
    {
        try
        {
            var conta = await _financeiro.CriarContaReceberAsync(req, ct);
            return CreatedAtAction(nameof(ObterContaReceber), new { id = conta.Id }, conta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("contas-receber/{id:int}/receber")]
    public async Task<ActionResult<ContaReceberResponse>> ReceberConta(int id, [FromBody] ReceberContaRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _financeiro.ReceberContaAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("contas-receber/{id:int}/cancelar")]
    public async Task<ActionResult<ContaReceberResponse>> CancelarContaReceber(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _financeiro.CancelarContaReceberAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ── Contas a Pagar ────────────────────────────────────────────────────────

    [HttpGet("contas-pagar")]
    public async Task<ActionResult<IEnumerable<ContaPagarResponse>>> ListarContasPagar(CancellationToken ct)
        => Ok(await _financeiro.ListarContasPagarAsync(ct));

    [HttpPost("contas-pagar/{id:int}/pagar")]
    public async Task<ActionResult<ContaPagarResponse>> PagarConta(int id, [FromBody] PagarContaRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _financeiro.PagarContaAsync(id, req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ── Lançamentos Manuais ───────────────────────────────────────────────────

    [HttpGet("lancamentos")]
    public async Task<ActionResult<IEnumerable<LancamentoFinanceiroResponse>>> ListarLancamentos(CancellationToken ct)
        => Ok(await _financeiro.ListarLancamentosAsync(ct));

    [HttpPost("lancamentos")]
    public async Task<ActionResult<LancamentoFinanceiroResponse>> CriarLancamento([FromBody] LancamentoFinanceiroRequest req, CancellationToken ct)
    {
        try
        {
            var lancamento = await _financeiro.CriarLancamentoAsync(req, ct);
            return Created(string.Empty, lancamento);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // ── Relatórios ────────────────────────────────────────────────────────────

    [HttpGet("fluxo-caixa")]
    public async Task<ActionResult<FluxoCaixaResponse>> FluxoCaixa(
        [FromQuery] DateTime inicio, [FromQuery] DateTime fim, CancellationToken ct)
    {
        if (fim < inicio)
            return BadRequest(new { erro = "A data fim deve ser maior ou igual à data início." });

        return Ok(await _financeiro.ObterFluxoCaixaAsync(inicio, fim, ct));
    }

    [HttpGet("dre")]
    public async Task<ActionResult<DreResponse>> Dre([FromQuery] int ano, [FromQuery] int mes, CancellationToken ct)
    {
        if (mes < 1 || mes > 12)
            return BadRequest(new { erro = "Mês deve ser entre 1 e 12." });

        return Ok(await _financeiro.ObterDreAsync(ano, mes, ct));
    }
}
