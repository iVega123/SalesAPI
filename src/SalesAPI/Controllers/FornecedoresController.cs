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
public class FornecedoresController : ControllerBase
{
    private readonly IAppDbContext _db;

    public FornecedoresController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FornecedorResponse>>> Listar(CancellationToken ct)
    {
        var lista = await _db.Fornecedores.AsNoTracking().ToListAsync(ct);
        return Ok(lista.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FornecedorResponse>> Obter(int id, CancellationToken ct)
    {
        var f = await _db.Fornecedores.FindAsync([id], ct);
        if (f is null) return NotFound();
        return Ok(Map(f));
    }

    [HttpPost]
    public async Task<ActionResult<FornecedorResponse>> Criar([FromBody] FornecedorRequest req, CancellationToken ct)
    {
        var f = FromRequest(req);
        _db.Fornecedores.Add(f);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = f.Id }, Map(f));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] FornecedorRequest req, CancellationToken ct)
    {
        var f = await _db.Fornecedores.FindAsync([id], ct);
        if (f is null) return NotFound();
        AplicarRequest(f, req);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var f = await _db.Fornecedores.FindAsync([id], ct);
        if (f is null) return NotFound();
        f.Status = StatusFornecedor.Inativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Contatos ──────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/contatos")]
    public async Task<ActionResult<IEnumerable<ContatoFornecedorResponse>>> ListarContatos(int id, CancellationToken ct)
    {
        var contatos = await _db.ContatosFornecedores
            .Where(c => c.FornecedorId == id)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(contatos.Select(MapContato));
    }

    [HttpPost("{id:int}/contatos")]
    public async Task<ActionResult<ContatoFornecedorResponse>> AdicionarContato(
        int id, [FromBody] ContatoFornecedorRequest req, CancellationToken ct)
    {
        if (!await _db.Fornecedores.AnyAsync(f => f.Id == id, ct))
            return NotFound();

        if (req.Principal)
            await DesmarcarPrincipal(id, ct);

        var contato = new ContatoFornecedor
        {
            FornecedorId = id,
            Nome = req.Nome,
            Cargo = req.Cargo,
            Email = req.Email,
            Telefone = req.Telefone,
            Principal = req.Principal
        };

        _db.ContatosFornecedores.Add(contato);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListarContatos), new { id }, MapContato(contato));
    }

    [HttpPut("{id:int}/contatos/{contatoId:int}")]
    public async Task<IActionResult> AtualizarContato(int id, int contatoId, [FromBody] ContatoFornecedorRequest req, CancellationToken ct)
    {
        var c = await _db.ContatosFornecedores.FirstOrDefaultAsync(c => c.Id == contatoId && c.FornecedorId == id, ct);
        if (c is null) return NotFound();

        if (req.Principal && !c.Principal)
            await DesmarcarPrincipal(id, ct);

        c.Nome = req.Nome;
        c.Cargo = req.Cargo;
        c.Email = req.Email;
        c.Telefone = req.Telefone;
        c.Principal = req.Principal;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/contatos/{contatoId:int}")]
    public async Task<IActionResult> RemoverContato(int id, int contatoId, CancellationToken ct)
    {
        var c = await _db.ContatosFornecedores.FirstOrDefaultAsync(c => c.Id == contatoId && c.FornecedorId == id, ct);
        if (c is null) return NotFound();
        _db.ContatosFornecedores.Remove(c);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task DesmarcarPrincipal(int fornecedorId, CancellationToken ct)
    {
        var principal = await _db.ContatosFornecedores
            .FirstOrDefaultAsync(c => c.FornecedorId == fornecedorId && c.Principal, ct);
        if (principal is not null)
            principal.Principal = false;
    }

    private static Fornecedor FromRequest(FornecedorRequest req) => new()
    {
        RazaoSocial = req.RazaoSocial,
        NomeFantasia = req.NomeFantasia ?? string.Empty,
        CNPJ = req.CNPJ,
        IE = req.IE,
        Email = req.Email,
        Telefone = req.Telefone,
        Site = req.Site,
        CondicoesPagamento = req.CondicoesPagamento,
        Observacoes = req.Observacoes
    };

    private static void AplicarRequest(Fornecedor f, FornecedorRequest req)
    {
        f.RazaoSocial = req.RazaoSocial;
        f.NomeFantasia = req.NomeFantasia ?? string.Empty;
        f.CNPJ = req.CNPJ;
        f.IE = req.IE;
        f.Email = req.Email;
        f.Telefone = req.Telefone;
        f.Site = req.Site;
        f.CondicoesPagamento = req.CondicoesPagamento;
        f.Observacoes = req.Observacoes;
    }

    private static FornecedorResponse Map(Fornecedor f) => new(
        f.Id, f.RazaoSocial, f.NomeFantasia, f.CNPJ, f.IE, f.Email,
        f.Telefone, f.Site, f.CondicoesPagamento, f.Status, f.CriadoEm);

    private static ContatoFornecedorResponse MapContato(ContatoFornecedor c) => new(
        c.Id, c.FornecedorId, c.Nome, c.Cargo, c.Email, c.Telefone, c.Principal);
}
