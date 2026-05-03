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
public class MarcasController : ControllerBase
{
    private readonly IAppDbContext _db;

    public MarcasController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarcaResponse>>> Listar(CancellationToken ct)
    {
        var lista = await _db.Marcas.AsNoTracking().ToListAsync(ct);
        return Ok(lista.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MarcaResponse>> Obter(int id, CancellationToken ct)
    {
        var m = await _db.Marcas.FindAsync([id], ct);
        if (m is null) return NotFound();
        return Ok(Map(m));
    }

    [HttpPost]
    public async Task<ActionResult<MarcaResponse>> Criar([FromBody] MarcaRequest req, CancellationToken ct)
    {
        var m = new Marca { Nome = req.Nome, Ativo = req.Ativo };
        _db.Marcas.Add(m);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = m.Id }, Map(m));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] MarcaRequest req, CancellationToken ct)
    {
        var m = await _db.Marcas.FindAsync([id], ct);
        if (m is null) return NotFound();
        m.Nome = req.Nome;
        m.Ativo = req.Ativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var m = await _db.Marcas.FindAsync([id], ct);
        if (m is null) return NotFound();
        m.Ativo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static MarcaResponse Map(Marca m) => new(m.Id, m.Nome, m.Ativo, m.CriadoEm);
}
