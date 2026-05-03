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
public class CategoriasController : ControllerBase
{
    private readonly IAppDbContext _db;

    public CategoriasController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> Listar(CancellationToken ct)
    {
        var lista = await _db.Categorias.AsNoTracking().ToListAsync(ct);
        return Ok(lista.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaResponse>> Obter(int id, CancellationToken ct)
    {
        var c = await _db.Categorias.FindAsync([id], ct);
        if (c is null) return NotFound();
        return Ok(Map(c));
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaResponse>> Criar([FromBody] CategoriaRequest req, CancellationToken ct)
    {
        var c = new Categoria { Nome = req.Nome, Descricao = req.Descricao ?? string.Empty, Ativo = req.Ativo };
        _db.Categorias.Add(c);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = c.Id }, Map(c));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] CategoriaRequest req, CancellationToken ct)
    {
        var c = await _db.Categorias.FindAsync([id], ct);
        if (c is null) return NotFound();
        c.Nome = req.Nome;
        c.Descricao = req.Descricao ?? string.Empty;
        c.Ativo = req.Ativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var c = await _db.Categorias.FindAsync([id], ct);
        if (c is null) return NotFound();
        c.Ativo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CategoriaResponse Map(Categoria c) => new(c.Id, c.Nome, c.Descricao, c.Ativo, c.CriadoEm);
}
