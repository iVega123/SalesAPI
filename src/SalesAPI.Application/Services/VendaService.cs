using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface IVendaService
{
    Task<VendaResponse> CriarAsync(VendaRequest request, CancellationToken ct = default);
    Task<VendaResponse?> ObterAsync(int id, CancellationToken ct = default);
    Task<List<VendaResponse>> ListarAsync(CancellationToken ct = default);
    Task<VendaResponse> CancelarAsync(int id, CancellationToken ct = default);
    Task<ParcelaVendaResponse> PagarParcelaAsync(int vendaId, int parcelaId, PagarParcelaRequest request, CancellationToken ct = default);
}

public class VendaService : IVendaService
{
    private readonly IAppDbContext _db;
    private readonly IEstoqueService _estoque;

    public VendaService(IAppDbContext db, IEstoqueService estoque)
    {
        _db = db;
        _estoque = estoque;
    }

    public async Task<VendaResponse> CriarAsync(VendaRequest request, CancellationToken ct = default)
    {
        _ = await _db.Clientes.FindAsync([request.ClienteId], ct)
            ?? throw new InvalidOperationException($"Cliente {request.ClienteId} não encontrado.");

        if (request.VendedorId.HasValue)
            _ = await _db.Usuarios.FindAsync([request.VendedorId.Value], ct)
                ?? throw new InvalidOperationException($"Vendedor {request.VendedorId} não encontrado.");

        if (request.Itens is null || request.Itens.Count == 0)
            throw new InvalidOperationException("Venda precisa de ao menos um item.");

        if (request.NumeroParcelas < 1)
            throw new InvalidOperationException("Número de parcelas deve ser ao menos 1.");

        var itensAgrupados = request.Itens
            .GroupBy(i => i.ProdutoId)
            .Select(g => new { ProdutoId = g.Key, Quantidade = g.Sum(x => x.Quantidade) })
            .ToList();

        var produtoIds = itensAgrupados.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos
            .Include(p => p.Estoque)
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        if (produtos.Count != produtoIds.Count)
        {
            var faltantes = produtoIds.Except(produtos.Select(p => p.Id));
            throw new InvalidOperationException($"Produto(s) não encontrado(s): {string.Join(", ", faltantes)}");
        }

        foreach (var item in itensAgrupados)
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            var disponivel = produto.Estoque?.Quantidade ?? 0;
            if (disponivel < item.Quantidade)
                throw new InvalidOperationException(
                    $"Estoque insuficiente para '{produto.Nome}'. Disponível: {disponivel}, solicitado: {item.Quantidade}.");
        }

        var itensVenda = itensAgrupados.Select(item =>
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            return new ItemVenda
            {
                ProdutoId = produto.Id,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.PrecoVenda
            };
        }).ToList();

        var subtotal = itensVenda.Sum(i => i.PrecoUnitario * i.Quantidade);
        var desconto = Math.Min(request.Desconto, subtotal);
        var total = subtotal - desconto;

        var venda = new Venda
        {
            ClienteId = request.ClienteId,
            VendedorId = request.VendedorId,
            Status = StatusVenda.Confirmada,
            DataVenda = DateTime.UtcNow,
            Subtotal = subtotal,
            Desconto = desconto,
            Total = total,
            Observacoes = request.Observacoes,
            Itens = itensVenda
        };

        var valorParcela = Math.Round(total / request.NumeroParcelas, 2);
        var diferenca = total - valorParcela * request.NumeroParcelas;
        for (int i = 1; i <= request.NumeroParcelas; i++)
        {
            var valor = i == request.NumeroParcelas ? valorParcela + diferenca : valorParcela;
            venda.Parcelas.Add(new ParcelaVenda
            {
                Numero = i,
                TotalParcelas = request.NumeroParcelas,
                Valor = valor,
                Vencimento = request.PrimeiroVencimento.AddMonths(i - 1)
            });
        }

        if (request.VendedorId.HasValue)
        {
            const decimal percentualPadrao = 5m;
            venda.Comissoes.Add(new Comissao
            {
                VendedorId = request.VendedorId.Value,
                PercentualComissao = percentualPadrao,
                ValorComissao = Math.Round(total * percentualPadrao / 100m, 2)
            });
        }

        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync(ct);

        foreach (var item in itensVenda)
        {
            await _estoque.RegistrarMovimentacaoInternaAsync(
                item.ProdutoId, TipoMovimentacao.Saida, item.Quantidade,
                $"Venda #{venda.Id}", new MovimentacaoContexto(VendaId: venda.Id), ct);
        }

        return await ObterAsync(venda.Id, ct)
            ?? throw new InvalidOperationException("Falha ao recuperar venda recém-criada.");
    }

    public async Task<VendaResponse?> ObterAsync(int id, CancellationToken ct = default)
    {
        var v = await _db.Vendas
            .Include(x => x.Cliente)
            .Include(x => x.Vendedor)
            .Include(x => x.Itens).ThenInclude(i => i.Produto)
            .Include(x => x.Parcelas)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return v is null ? null : Map(v);
    }

    public async Task<List<VendaResponse>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _db.Vendas
            .Include(x => x.Cliente)
            .Include(x => x.Vendedor)
            .Include(x => x.Itens).ThenInclude(i => i.Produto)
            .Include(x => x.Parcelas)
            .AsNoTracking()
            .OrderByDescending(x => x.DataVenda)
            .ToListAsync(ct);
        return lista.Select(Map).ToList();
    }

    public async Task<VendaResponse> CancelarAsync(int id, CancellationToken ct = default)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Cliente)
            .Include(v => v.Vendedor)
            .Include(v => v.Parcelas)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new InvalidOperationException($"Venda {id} não encontrada.");

        if (venda.Status == StatusVenda.Cancelada)
            throw new InvalidOperationException("Venda já cancelada.");

        venda.Status = StatusVenda.Cancelada;

        foreach (var parcela in venda.Parcelas.Where(p => p.Status == StatusParcela.Pendente))
            parcela.Status = StatusParcela.Cancelada;

        foreach (var item in venda.Itens)
        {
            await _estoque.RegistrarMovimentacaoInternaAsync(
                item.ProdutoId, TipoMovimentacao.Entrada, item.Quantidade,
                $"Cancelamento da venda #{id}", new MovimentacaoContexto(VendaId: id), ct);
        }

        await _db.SaveChangesAsync(ct);
        return Map(venda);
    }

    public async Task<ParcelaVendaResponse> PagarParcelaAsync(int vendaId, int parcelaId, PagarParcelaRequest request, CancellationToken ct = default)
    {
        var parcela = await _db.ParcelasVenda
            .FirstOrDefaultAsync(p => p.Id == parcelaId && p.VendaId == vendaId, ct)
            ?? throw new InvalidOperationException($"Parcela {parcelaId} não encontrada na venda {vendaId}.");

        if (parcela.Status == StatusParcela.Paga)
            throw new InvalidOperationException("Parcela já paga.");

        if (parcela.Status == StatusParcela.Cancelada)
            throw new InvalidOperationException("Parcela cancelada não pode ser paga.");

        parcela.Status = StatusParcela.Paga;
        parcela.PagamentoEm = request.PagamentoEm ?? DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapParcela(parcela);
    }

    private static VendaResponse Map(Venda v) => new(
        v.Id,
        v.ClienteId,
        v.Cliente?.Nome ?? string.Empty,
        v.VendedorId,
        v.Vendedor?.Nome,
        v.Status,
        v.DataVenda,
        v.Subtotal,
        v.Desconto,
        v.Total,
        v.Observacoes,
        v.Itens.Select(i => new ItemVendaResponse(
            i.ProdutoId, i.Produto?.Nome ?? string.Empty,
            i.Quantidade, i.PrecoUnitario, i.PrecoUnitario * i.Quantidade)).ToList(),
        v.Parcelas.OrderBy(p => p.Numero).Select(MapParcela).ToList());

    private static ParcelaVendaResponse MapParcela(ParcelaVenda p) => new(
        p.Id, p.Numero, p.TotalParcelas, p.Valor, p.Vencimento, p.PagamentoEm, p.Status);
}
