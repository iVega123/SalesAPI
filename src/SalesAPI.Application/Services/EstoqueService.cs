using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public record MovimentacaoContexto(
    int? UsuarioId = null,
    int? CompraId = null,
    int? VendaId = null,
    int? VendaPdvId = null,
    int? QuantidadeAbsoluta = null);

public interface IEstoqueService
{
    Task<MovimentacaoEstoqueResponse> EntradaAsync(EntradaEstoqueRequest request, CancellationToken ct = default);
    Task<MovimentacaoEstoqueResponse> SaidaAsync(SaidaEstoqueRequest request, CancellationToken ct = default);
    Task<MovimentacaoEstoqueResponse> AjustarAsync(AjusteEstoqueMovRequest request, CancellationToken ct = default);
    Task<List<MovimentacaoEstoqueResponse>> ListarMovimentacoesAsync(int produtoId, CancellationToken ct = default);
    Task<List<AlertaEstoqueResponse>> AlertasAbaixoMinimoAsync(CancellationToken ct = default);
    Task<InventarioResponse> IniciarInventarioAsync(IniciarInventarioRequest request, CancellationToken ct = default);
    Task<InventarioResponse> RegistrarAjusteInventarioAsync(int inventarioId, RegistrarAjusteRequest request, CancellationToken ct = default);
    Task<InventarioResponse> FinalizarInventarioAsync(int inventarioId, CancellationToken ct = default);

    Task<MovimentacaoEstoque> RegistrarMovimentacaoInternaAsync(
        int produtoId, TipoMovimentacao tipo, int quantidade, string? motivo,
        MovimentacaoContexto? contexto = null, CancellationToken ct = default);
}

public class EstoqueService : IEstoqueService
{
    private readonly IAppDbContext _db;

    public EstoqueService(IAppDbContext db) => _db = db;

    public async Task<MovimentacaoEstoqueResponse> EntradaAsync(EntradaEstoqueRequest request, CancellationToken ct = default)
    {
        var mov = await RegistrarMovimentacaoInternaAsync(
            request.ProdutoId, TipoMovimentacao.Entrada, request.Quantidade,
            request.Motivo, new MovimentacaoContexto(CompraId: request.CompraId), ct);
        return MapMov(mov);
    }

    public async Task<MovimentacaoEstoqueResponse> SaidaAsync(SaidaEstoqueRequest request, CancellationToken ct = default)
    {
        var mov = await RegistrarMovimentacaoInternaAsync(
            request.ProdutoId, TipoMovimentacao.Saida, request.Quantidade,
            request.Motivo, ct: ct);
        return MapMov(mov);
    }

    public async Task<MovimentacaoEstoqueResponse> AjustarAsync(AjusteEstoqueMovRequest request, CancellationToken ct = default)
    {
        var estoque = await ObterOuCriarEstoqueAsync(request.ProdutoId, ct);
        var anterior = estoque.Quantidade;
        var diferenca = Math.Abs(request.NovaQuantidade - anterior);

        var mov = await RegistrarMovimentacaoInternaAsync(
            request.ProdutoId, TipoMovimentacao.Ajuste, diferenca,
            request.Motivo, new MovimentacaoContexto(QuantidadeAbsoluta: request.NovaQuantidade), ct);
        return MapMov(mov);
    }

    public async Task<List<MovimentacaoEstoqueResponse>> ListarMovimentacoesAsync(int produtoId, CancellationToken ct = default)
    {
        var lista = await _db.MovimentacoesEstoque
            .Include(m => m.Produto)
            .Where(m => m.ProdutoId == produtoId)
            .OrderByDescending(m => m.CriadoEm)
            .AsNoTracking()
            .ToListAsync(ct);
        return lista.Select(MapMov).ToList();
    }

    public async Task<List<AlertaEstoqueResponse>> AlertasAbaixoMinimoAsync(CancellationToken ct = default)
    {
        var alertas = await _db.Estoques
            .Include(e => e.Produto)
            .Where(e => e.Produto != null && e.Quantidade <= e.Produto.EstoqueMinimo)
            .AsNoTracking()
            .ToListAsync(ct);

        return alertas.Select(e => new AlertaEstoqueResponse(
            e.ProdutoId,
            e.Produto!.Nome,
            e.Quantidade,
            e.Produto.EstoqueMinimo)).ToList();
    }

    public async Task<InventarioResponse> IniciarInventarioAsync(IniciarInventarioRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FindAsync([request.UsuarioId], ct)
            ?? throw new InvalidOperationException($"Usuário {request.UsuarioId} não encontrado.");

        var inventario = new Inventario
        {
            Descricao = request.Descricao,
            UsuarioId = usuario.Id
        };

        _db.Inventarios.Add(inventario);
        await _db.SaveChangesAsync(ct);
        return MapInventario(inventario);
    }

    public async Task<InventarioResponse> RegistrarAjusteInventarioAsync(int inventarioId, RegistrarAjusteRequest request, CancellationToken ct = default)
    {
        var inventario = await _db.Inventarios
            .Include(i => i.Ajustes).ThenInclude(a => a.Produto)
            .FirstOrDefaultAsync(i => i.Id == inventarioId, ct)
            ?? throw new InvalidOperationException($"Inventário {inventarioId} não encontrado.");

        if (inventario.Finalizado)
            throw new InvalidOperationException("Inventário já finalizado.");

        var estoque = await ObterOuCriarEstoqueAsync(request.ProdutoId, ct);

        if (!await _db.Produtos.AnyAsync(p => p.Id == request.ProdutoId, ct))
            throw new InvalidOperationException($"Produto {request.ProdutoId} não encontrado.");

        var ajuste = inventario.Ajustes.FirstOrDefault(a => a.ProdutoId == request.ProdutoId);
        if (ajuste is null)
        {
            ajuste = new AjusteEstoque
            {
                InventarioId = inventarioId,
                ProdutoId = request.ProdutoId,
                QuantidadeSistema = estoque.Quantidade
            };
            inventario.Ajustes.Add(ajuste);
        }

        ajuste.QuantidadeContada = request.QuantidadeContada;
        ajuste.Motivo = request.Motivo;

        await _db.SaveChangesAsync(ct);
        return MapInventario(inventario);
    }

    public async Task<InventarioResponse> FinalizarInventarioAsync(int inventarioId, CancellationToken ct = default)
    {
        var inventario = await _db.Inventarios
            .Include(i => i.Ajustes).ThenInclude(a => a.Produto)
            .FirstOrDefaultAsync(i => i.Id == inventarioId, ct)
            ?? throw new InvalidOperationException($"Inventário {inventarioId} não encontrado.");

        if (inventario.Finalizado)
            throw new InvalidOperationException("Inventário já finalizado.");

        foreach (var ajuste in inventario.Ajustes.Where(a => a.Diferenca != 0))
        {
            await RegistrarMovimentacaoInternaAsync(
                ajuste.ProdutoId, TipoMovimentacao.Ajuste, Math.Abs(ajuste.Diferenca),
                ajuste.Motivo ?? "Ajuste de inventário",
                new MovimentacaoContexto(UsuarioId: inventario.UsuarioId, QuantidadeAbsoluta: ajuste.QuantidadeContada),
                ct);
        }

        inventario.Finalizado = true;
        inventario.FinalizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapInventario(inventario);
    }

    public async Task<MovimentacaoEstoque> RegistrarMovimentacaoInternaAsync(
        int produtoId, TipoMovimentacao tipo, int quantidade, string? motivo,
        MovimentacaoContexto? contexto = null, CancellationToken ct = default)
    {
        var ctx = contexto ?? new MovimentacaoContexto();
        var estoque = await ObterOuCriarEstoqueAsync(produtoId, ct);
        var anterior = estoque.Quantidade;

        int resultante;
        if (ctx.QuantidadeAbsoluta.HasValue)
        {
            resultante = ctx.QuantidadeAbsoluta.Value;
        }
        else if (tipo == TipoMovimentacao.Entrada)
        {
            resultante = anterior + quantidade;
        }
        else
        {
            if (anterior < quantidade)
                throw new InvalidOperationException(
                    $"Estoque insuficiente para produto {produtoId}. Disponível: {anterior}, solicitado: {quantidade}.");
            resultante = anterior - quantidade;
        }

        estoque.Quantidade = resultante;
        estoque.AtualizadoEm = DateTime.UtcNow;

        var mov = new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = tipo,
            Quantidade = quantidade,
            QuantidadeAnterior = anterior,
            QuantidadeResultante = resultante,
            Motivo = motivo,
            UsuarioId = ctx.UsuarioId,
            CompraId = ctx.CompraId,
            VendaId = ctx.VendaId
        };

        _db.MovimentacoesEstoque.Add(mov);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(mov).Reference(m => m.Produto).LoadAsync(ct);
        return mov;
    }

    private async Task<Estoque> ObterOuCriarEstoqueAsync(int produtoId, CancellationToken ct)
    {
        var estoque = await _db.Estoques.FirstOrDefaultAsync(e => e.ProdutoId == produtoId, ct);
        if (estoque is not null) return estoque;

        if (!await _db.Produtos.AnyAsync(p => p.Id == produtoId, ct))
            throw new InvalidOperationException($"Produto {produtoId} não encontrado.");

        estoque = new Estoque { ProdutoId = produtoId, Quantidade = 0 };
        _db.Estoques.Add(estoque);
        await _db.SaveChangesAsync(ct);
        return estoque;
    }

    private static MovimentacaoEstoqueResponse MapMov(MovimentacaoEstoque m) => new(
        m.Id, m.ProdutoId, m.Produto?.Nome ?? string.Empty, m.Tipo,
        m.Quantidade, m.QuantidadeAnterior, m.QuantidadeResultante, m.Motivo, m.CriadoEm);

    private static InventarioResponse MapInventario(Inventario i) => new(
        i.Id, i.Descricao, i.IniciadoEm, i.FinalizadoEm, i.Finalizado,
        i.Ajustes.Select(a => new AjusteEstoqueResponse(
            a.ProdutoId, a.Produto?.Nome ?? string.Empty,
            a.QuantidadeContada, a.QuantidadeSistema, a.Diferenca, a.Motivo)).ToList());
}
