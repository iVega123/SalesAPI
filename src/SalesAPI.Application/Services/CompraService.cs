using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface ICompraService
{
    Task<CompraResponse> CriarAsync(CompraRequest request, CancellationToken ct = default);
    Task<CompraResponse?> ObterAsync(int id, CancellationToken ct = default);
    Task<List<CompraResponse>> ListarAsync(CancellationToken ct = default);
    Task<CompraResponse> ReceberAsync(int compraId, ReceberCompraRequest request, CancellationToken ct = default);
    Task<CompraResponse> GerarParcelasAsync(int compraId, GerarParcelasCompraRequest request, CancellationToken ct = default);
    Task<CompraResponse> CancelarAsync(int compraId, CancellationToken ct = default);
}

public class CompraService : ICompraService
{
    private readonly IAppDbContext _db;
    private readonly IEstoqueService _estoque;

    public CompraService(IAppDbContext db, IEstoqueService estoque)
    {
        _db = db;
        _estoque = estoque;
    }

    public async Task<CompraResponse> CriarAsync(CompraRequest request, CancellationToken ct = default)
    {
        _ = await _db.Fornecedores.FindAsync([request.FornecedorId], ct)
            ?? throw new InvalidOperationException($"Fornecedor {request.FornecedorId} não encontrado.");

        _ = await _db.Usuarios.FindAsync([request.UsuarioId], ct)
            ?? throw new InvalidOperationException($"Usuário {request.UsuarioId} não encontrado.");

        var produtoIds = request.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await _db.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        if (produtos.Count != produtoIds.Count)
        {
            var faltantes = produtoIds.Except(produtos.Select(p => p.Id));
            throw new InvalidOperationException($"Produto(s) não encontrado(s): {string.Join(", ", faltantes)}");
        }

        var itens = request.Itens.Select(i => new ItemCompra
        {
            ProdutoId = i.ProdutoId,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        var subtotal = itens.Sum(i => i.PrecoUnitario * i.Quantidade);
        var total = subtotal + request.Frete + request.Impostos;

        var compra = new Compra
        {
            FornecedorId = request.FornecedorId,
            UsuarioId = request.UsuarioId,
            DataPrevisaoEntrega = request.DataPrevisaoEntrega,
            Frete = request.Frete,
            Impostos = request.Impostos,
            Subtotal = subtotal,
            Total = total,
            NumeroNF = request.NumeroNF,
            Observacoes = request.Observacoes,
            Itens = itens
        };

        _db.Compras.Add(compra);
        await _db.SaveChangesAsync(ct);

        return await ObterAsync(compra.Id, ct)
            ?? throw new InvalidOperationException("Falha ao recuperar compra recém-criada.");
    }

    public async Task<CompraResponse?> ObterAsync(int id, CancellationToken ct = default)
    {
        var compra = await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .Include(c => c.ContasPagar)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return compra is null ? null : Map(compra);
    }

    public async Task<List<CompraResponse>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .Include(c => c.ContasPagar)
            .AsNoTracking()
            .OrderByDescending(c => c.DataCompra)
            .ToListAsync(ct);

        return lista.Select(Map).ToList();
    }

    public async Task<CompraResponse> ReceberAsync(int compraId, ReceberCompraRequest request, CancellationToken ct = default)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Fornecedor)
            .Include(c => c.ContasPagar)
            .FirstOrDefaultAsync(c => c.Id == compraId, ct)
            ?? throw new InvalidOperationException($"Compra {compraId} não encontrada.");

        if (compra.Status == StatusCompra.Cancelada)
            throw new InvalidOperationException("Compra cancelada não pode ser recebida.");

        _ = await _db.Usuarios.FindAsync([request.UsuarioId], ct)
            ?? throw new InvalidOperationException($"Usuário {request.UsuarioId} não encontrado.");

        var recebimento = new RecebimentoCompra
        {
            CompraId = compraId,
            UsuarioId = request.UsuarioId,
            Observacoes = request.Observacoes
        };

        foreach (var itemReq in request.Itens)
        {
            var itemCompra = compra.Itens.FirstOrDefault(i => i.Id == itemReq.ItemCompraId)
                ?? throw new InvalidOperationException($"Item {itemReq.ItemCompraId} não pertence à compra {compraId}.");

            var pendente = itemCompra.Quantidade - itemCompra.QuantidadeRecebida;
            if (itemReq.QuantidadeRecebida > pendente)
                throw new InvalidOperationException(
                    $"Item {itemReq.ItemCompraId}: quantidade a receber ({itemReq.QuantidadeRecebida}) excede o pendente ({pendente}).");

            recebimento.Itens.Add(new ItemRecebimento
            {
                ItemCompraId = itemReq.ItemCompraId,
                QuantidadeRecebida = itemReq.QuantidadeRecebida
            });

            itemCompra.QuantidadeRecebida += itemReq.QuantidadeRecebida;

            await _estoque.RegistrarMovimentacaoInternaAsync(
                itemCompra.ProdutoId, TipoMovimentacao.Entrada, itemReq.QuantidadeRecebida,
                $"Recebimento da compra #{compraId}",
                new MovimentacaoContexto(UsuarioId: request.UsuarioId, CompraId: compraId), ct);
        }

        _db.RecebimentosCompra.Add(recebimento);

        var totalRecebido = compra.Itens.All(i => i.QuantidadeRecebida >= i.Quantidade);
        compra.Status = totalRecebido ? StatusCompra.Recebida : StatusCompra.Confirmada;

        await _db.SaveChangesAsync(ct);

        return await ObterAsync(compraId, ct)
            ?? throw new InvalidOperationException("Falha ao recuperar compra após recebimento.");
    }

    public async Task<CompraResponse> GerarParcelasAsync(int compraId, GerarParcelasCompraRequest request, CancellationToken ct = default)
    {
        var compra = await _db.Compras
            .Include(c => c.ContasPagar)
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(c => c.Id == compraId, ct)
            ?? throw new InvalidOperationException($"Compra {compraId} não encontrada.");

        if (compra.Status == StatusCompra.Cancelada)
            throw new InvalidOperationException("Compra cancelada.");

        if (compra.ContasPagar.Count > 0)
            throw new InvalidOperationException("Parcelas já geradas para esta compra.");

        var valorParcela = Math.Round(compra.Total / request.NumeroParcelas, 2);
        var diferenca = compra.Total - valorParcela * request.NumeroParcelas;

        for (int i = 1; i <= request.NumeroParcelas; i++)
        {
            var valor = i == request.NumeroParcelas ? valorParcela + diferenca : valorParcela;
            compra.ContasPagar.Add(new ContaPagar
            {
                CompraId = compraId,
                Parcela = i,
                TotalParcelas = request.NumeroParcelas,
                Valor = valor,
                Vencimento = request.PrimeiroVencimento.AddMonths(i - 1)
            });
        }

        await _db.SaveChangesAsync(ct);

        return await ObterAsync(compraId, ct)
            ?? throw new InvalidOperationException("Falha ao recuperar compra após gerar parcelas.");
    }

    public async Task<CompraResponse> CancelarAsync(int compraId, CancellationToken ct = default)
    {
        var compra = await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .Include(c => c.ContasPagar)
            .FirstOrDefaultAsync(c => c.Id == compraId, ct)
            ?? throw new InvalidOperationException($"Compra {compraId} não encontrada.");

        if (compra.Status == StatusCompra.Recebida)
            throw new InvalidOperationException("Compra já recebida não pode ser cancelada.");

        compra.Status = StatusCompra.Cancelada;

        foreach (var conta in compra.ContasPagar.Where(c => c.Status == StatusContaPagar.Pendente))
            conta.Status = StatusContaPagar.Cancelada;

        await _db.SaveChangesAsync(ct);

        return Map(compra);
    }

    private static CompraResponse Map(Compra c) => new(
        c.Id,
        c.FornecedorId,
        c.Fornecedor?.RazaoSocial ?? string.Empty,
        c.Status,
        c.DataCompra,
        c.DataPrevisaoEntrega,
        c.Subtotal,
        c.Frete,
        c.Impostos,
        c.Total,
        c.NumeroNF,
        c.Observacoes,
        c.Itens.Select(i => new ItemCompraResponse(
            i.Id,
            i.ProdutoId,
            i.Produto?.Nome ?? string.Empty,
            i.Quantidade,
            i.QuantidadeRecebida,
            i.PrecoUnitario,
            i.PrecoUnitario * i.Quantidade)).ToList(),
        c.ContasPagar.OrderBy(p => p.Parcela).Select(p => new ContaPagarResponse(
            p.Id,
            p.Parcela,
            p.TotalParcelas,
            p.Valor,
            p.Vencimento,
            p.PagamentoEm,
            p.Status)).ToList(),
        c.CriadoEm);
}
