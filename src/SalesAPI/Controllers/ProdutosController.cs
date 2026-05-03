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
public class ProdutosController : ControllerBase
{
    private readonly IAppDbContext _db;

    public ProdutosController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoResponse>>> Listar(CancellationToken ct)
    {
        var produtos = await _db.Produtos
            .Include(p => p.Estoque)
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(produtos.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> Obter(int id, CancellationToken ct)
    {
        var p = await _db.Produtos
            .Include(p => p.Estoque)
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (p is null) return NotFound();
        return Ok(Map(p));
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoResponse>> Criar([FromBody] ProdutoRequest req, CancellationToken ct)
    {
        var p = FromRequest(req);
        p.Estoque = new Estoque { Quantidade = 0 };
        _db.Produtos.Add(p);
        await _db.SaveChangesAsync(ct);

        await RegistrarHistoricoPreco(p, "Cadastro inicial", ct);

        return CreatedAtAction(nameof(Obter), new { id = p.Id }, Map(p));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ProdutoRequest req, CancellationToken ct)
    {
        var p = await _db.Produtos.FindAsync([id], ct);
        if (p is null) return NotFound();

        var precoMudou = p.PrecoCusto != req.PrecoCusto || p.PrecoVenda != req.PrecoVenda;

        AplicarRequest(p, req);
        await _db.SaveChangesAsync(ct);

        if (precoMudou)
            await RegistrarHistoricoPreco(p, "Atualização", ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var p = await _db.Produtos.FindAsync([id], ct);
        if (p is null) return NotFound();

        p.Status = StatusProduto.Inativo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:int}/variacoes")]
    public async Task<ActionResult<IEnumerable<ProdutoVariacaoResponse>>> ListarVariacoes(int id, CancellationToken ct)
    {
        var variacoes = await _db.ProdutosVariacoes
            .Where(v => v.ProdutoId == id)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(variacoes.Select(MapVariacao));
    }

    [HttpPost("{id:int}/variacoes")]
    public async Task<ActionResult<ProdutoVariacaoResponse>> AdicionarVariacao(
        int id, [FromBody] ProdutoVariacaoRequest req, CancellationToken ct)
    {
        if (!await _db.Produtos.AnyAsync(p => p.Id == id, ct))
            return NotFound();

        var variacao = new ProdutoVariacao
        {
            ProdutoId = id,
            Tamanho = req.Tamanho,
            Cor = req.Cor,
            Modelo = req.Modelo,
            Material = req.Material,
            SKU = req.SKU,
            Preco = req.Preco,
            EstoqueQtd = req.EstoqueQtd
        };

        _db.ProdutosVariacoes.Add(variacao);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListarVariacoes), new { id }, MapVariacao(variacao));
    }

    [HttpGet("{id:int}/historico-precos")]
    public async Task<ActionResult<IEnumerable<ProdutoPrecoHistorico>>> HistoricoPrecos(int id, CancellationToken ct)
    {
        var historico = await _db.ProdutosPrecosHistorico
            .Where(h => h.ProdutoId == id)
            .OrderByDescending(h => h.RegistradoEm)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(historico);
    }

    private async Task RegistrarHistoricoPreco(Produto p, string motivo, CancellationToken ct)
    {
        _db.ProdutosPrecosHistorico.Add(new ProdutoPrecoHistorico
        {
            ProdutoId = p.Id,
            PrecoCusto = p.PrecoCusto,
            PrecoVenda = p.PrecoVenda,
            Motivo = motivo
        });
        await _db.SaveChangesAsync(ct);
    }

    private static Produto FromRequest(ProdutoRequest req) => new()
    {
        Nome = req.Nome,
        Descricao = req.Descricao ?? string.Empty,
        CodigoInterno = req.CodigoInterno,
        SKU = req.SKU,
        CodigoBarras = req.CodigoBarras,
        CategoriaId = req.CategoriaId,
        MarcaId = req.MarcaId,
        Unidade = req.Unidade ?? "UN",
        PrecoCusto = req.PrecoCusto,
        PrecoVenda = req.PrecoVenda,
        EstoqueMinimo = req.EstoqueMinimo,
        Status = req.Status,
        ImagemUrl = req.ImagemUrl,
        NCM = req.NCM,
        CFOP = req.CFOP,
        CST = req.CST,
        Origem = req.Origem,
        Aliquota = req.Aliquota
    };

    private static void AplicarRequest(Produto p, ProdutoRequest req)
    {
        p.Nome = req.Nome;
        p.Descricao = req.Descricao ?? string.Empty;
        p.CodigoInterno = req.CodigoInterno;
        p.SKU = req.SKU;
        p.CodigoBarras = req.CodigoBarras;
        p.CategoriaId = req.CategoriaId;
        p.MarcaId = req.MarcaId;
        p.Unidade = req.Unidade ?? "UN";
        p.PrecoCusto = req.PrecoCusto;
        p.PrecoVenda = req.PrecoVenda;
        p.EstoqueMinimo = req.EstoqueMinimo;
        p.Status = req.Status;
        p.ImagemUrl = req.ImagemUrl;
        p.NCM = req.NCM;
        p.CFOP = req.CFOP;
        p.CST = req.CST;
        p.Origem = req.Origem;
        p.Aliquota = req.Aliquota;
    }

    private static ProdutoResponse Map(Produto p) => new(
        p.Id, p.Nome, p.Descricao, p.CodigoInterno, p.SKU, p.CodigoBarras,
        p.CategoriaId, p.Categoria?.Nome, p.MarcaId, p.Marca?.Nome,
        p.Unidade, p.PrecoCusto, p.PrecoVenda, p.EstoqueMinimo, p.Status,
        p.ImagemUrl, p.NCM, p.CFOP, p.CST, p.Origem, p.Aliquota,
        p.Estoque?.Quantidade ?? 0, p.CriadoEm);

    private static ProdutoVariacaoResponse MapVariacao(ProdutoVariacao v) => new(
        v.Id, v.ProdutoId, v.Tamanho, v.Cor, v.Modelo, v.Material, v.SKU, v.Preco, v.EstoqueQtd, v.Ativo);
}
