using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IAppDbContext _db;

    public ClientesController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteResponse>>> Listar(CancellationToken ct)
    {
        var clientes = await _db.Clientes.AsNoTracking().ToListAsync(ct);
        return Ok(clientes.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteResponse>> Obter(int id, CancellationToken ct)
    {
        var c = await _db.Clientes.FindAsync([id], ct);
        if (c is null) return NotFound();
        return Ok(Map(c));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteResponse>> Criar([FromBody] ClienteRequest req, CancellationToken ct)
    {
        var c = FromRequest(req);
        _db.Clientes.Add(c);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = c.Id }, Map(c));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ClienteRequest req, CancellationToken ct)
    {
        var c = await _db.Clientes.FindAsync([id], ct);
        if (c is null) return NotFound();
        AplicarRequest(c, req);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var c = await _db.Clientes.FindAsync([id], ct);
        if (c is null) return NotFound();
        c.Status = StatusCliente.Inativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Endereços ─────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/enderecos")]
    public async Task<ActionResult<IEnumerable<EnderecoClienteResponse>>> ListarEnderecos(int id, CancellationToken ct)
    {
        var enderecos = await _db.EnderecosClientes
            .Where(e => e.ClienteId == id)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(enderecos.Select(MapEndereco));
    }

    [HttpPost("{id:int}/enderecos")]
    public async Task<ActionResult<EnderecoClienteResponse>> AdicionarEndereco(
        int id, [FromBody] EnderecoClienteRequest req, CancellationToken ct)
    {
        if (!await _db.Clientes.AnyAsync(c => c.Id == id, ct))
            return NotFound();

        if (req.Principal)
            await DesmarcarPrincipal(id, ct);

        var endereco = new EnderecoCliente
        {
            ClienteId = id,
            Logradouro = req.Logradouro,
            Numero = req.Numero ?? string.Empty,
            Complemento = req.Complemento,
            Bairro = req.Bairro,
            Cidade = req.Cidade,
            Estado = req.Estado,
            CEP = req.CEP,
            Tipo = req.Tipo,
            Principal = req.Principal
        };

        _db.EnderecosClientes.Add(endereco);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListarEnderecos), new { id }, MapEndereco(endereco));
    }

    [HttpPut("{id:int}/enderecos/{enderecoId:int}")]
    public async Task<IActionResult> AtualizarEndereco(int id, int enderecoId, [FromBody] EnderecoClienteRequest req, CancellationToken ct)
    {
        var e = await _db.EnderecosClientes.FirstOrDefaultAsync(e => e.Id == enderecoId && e.ClienteId == id, ct);
        if (e is null) return NotFound();

        if (req.Principal && !e.Principal)
            await DesmarcarPrincipal(id, ct);

        e.Logradouro = req.Logradouro;
        e.Numero = req.Numero ?? string.Empty;
        e.Complemento = req.Complemento;
        e.Bairro = req.Bairro;
        e.Cidade = req.Cidade;
        e.Estado = req.Estado;
        e.CEP = req.CEP;
        e.Tipo = req.Tipo;
        e.Principal = req.Principal;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/enderecos/{enderecoId:int}")]
    public async Task<IActionResult> RemoverEndereco(int id, int enderecoId, CancellationToken ct)
    {
        var e = await _db.EnderecosClientes.FirstOrDefaultAsync(e => e.Id == enderecoId && e.ClienteId == id, ct);
        if (e is null) return NotFound();
        _db.EnderecosClientes.Remove(e);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task DesmarcarPrincipal(int clienteId, CancellationToken ct)
    {
        var principal = await _db.EnderecosClientes
            .FirstOrDefaultAsync(e => e.ClienteId == clienteId && e.Principal, ct);
        if (principal is not null)
            principal.Principal = false;
    }

    private static Cliente FromRequest(ClienteRequest req) => new()
    {
        Nome = req.Nome,
        TipoPessoa = req.TipoPessoa,
        CPF = req.CPF,
        CNPJ = req.CNPJ,
        Email = req.Email,
        Telefone = req.Telefone,
        DataNascimento = req.DataNascimento,
        LimiteCredito = req.LimiteCredito,
        Observacoes = req.Observacoes
    };

    private static void AplicarRequest(Cliente c, ClienteRequest req)
    {
        c.Nome = req.Nome;
        c.TipoPessoa = req.TipoPessoa;
        c.CPF = req.CPF;
        c.CNPJ = req.CNPJ;
        c.Email = req.Email;
        c.Telefone = req.Telefone;
        c.DataNascimento = req.DataNascimento;
        c.LimiteCredito = req.LimiteCredito;
        c.Observacoes = req.Observacoes;
    }

    private static ClienteResponse Map(Cliente c) => new(
        c.Id, c.Nome, c.TipoPessoa, c.CPF, c.CNPJ, c.Email, c.Telefone,
        c.DataNascimento, c.LimiteCredito, c.PontosFidelidade, c.Status, c.CriadoEm);

    private static EnderecoClienteResponse MapEndereco(EnderecoCliente e) => new(
        e.Id, e.ClienteId, e.Logradouro, e.Numero, e.Complemento,
        e.Bairro, e.Cidade, e.Estado, e.CEP, e.Tipo, e.Principal);
}
