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
public class UsuariosController : ControllerBase
{
    private readonly IAppDbContext _db;

    public UsuariosController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar(CancellationToken ct)
    {
        var lista = await _db.Usuarios.Include(u => u.Perfil).AsNoTracking().ToListAsync(ct);
        return Ok(lista.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> Obter(int id, CancellationToken ct)
    {
        var u = await _db.Usuarios.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (u is null) return NotFound();
        return Ok(Map(u));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarUsuarioRequest req, CancellationToken ct)
    {
        var u = await _db.Usuarios.FindAsync([id], ct);
        if (u is null) return NotFound();

        if (!await _db.Perfis.AnyAsync(p => p.Id == req.PerfilId, ct))
            return BadRequest(new { mensagem = $"Perfil {req.PerfilId} não encontrado." });

        u.Nome = req.Nome;
        u.PerfilId = req.PerfilId;
        u.Ativo = req.Ativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var u = await _db.Usuarios.FindAsync([id], ct);
        if (u is null) return NotFound();
        u.Ativo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Perfis ────────────────────────────────────────────────────────────────

    [HttpGet("/api/perfis")]
    public async Task<ActionResult<IEnumerable<PerfilResponse>>> ListarPerfis(CancellationToken ct)
    {
        var lista = await _db.Perfis.AsNoTracking().ToListAsync(ct);
        return Ok(lista.Select(MapPerfil));
    }

    [HttpPost("/api/perfis")]
    public async Task<ActionResult<PerfilResponse>> CriarPerfil([FromBody] PerfilRequest req, CancellationToken ct)
    {
        var p = new Perfil { Nome = req.Nome, Descricao = req.Descricao ?? string.Empty, Ativo = req.Ativo };
        _db.Perfis.Add(p);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(ListarPerfis), MapPerfil(p));
    }

    [HttpPut("/api/perfis/{perfilId:int}")]
    public async Task<IActionResult> AtualizarPerfil(int perfilId, [FromBody] PerfilRequest req, CancellationToken ct)
    {
        var p = await _db.Perfis.FindAsync([perfilId], ct);
        if (p is null) return NotFound();
        p.Nome = req.Nome;
        p.Descricao = req.Descricao ?? string.Empty;
        p.Ativo = req.Ativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static UsuarioResponse Map(Usuario u) => new(
        u.Id, u.Nome, u.Email, u.PerfilId, u.Perfil?.Nome ?? string.Empty,
        u.Ativo, u.UltimoAcesso, u.CriadoEm);

    private static PerfilResponse MapPerfil(Perfil p) => new(p.Id, p.Nome, p.Descricao, p.Ativo);
}
