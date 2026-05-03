using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface IPdvService
{
    // Caixa
    Task<CaixaResponse> AbrirCaixaAsync(AbrirCaixaRequest request, CancellationToken ct = default);
    Task<CaixaResponse> FecharCaixaAsync(int caixaId, FecharCaixaRequest request, CancellationToken ct = default);
    Task<CaixaResponse?> ObterCaixaAsync(int id, CancellationToken ct = default);
    Task<List<CaixaResponse>> ListarCaixasAsync(CancellationToken ct = default);
    Task<MovimentoCaixaResponse> SangriaAsync(int caixaId, MovimentoCaixaRequest request, CancellationToken ct = default);
    Task<MovimentoCaixaResponse> SuprimentoAsync(int caixaId, MovimentoCaixaRequest request, CancellationToken ct = default);
    Task<List<MovimentoCaixaResponse>> ListarMovimentosAsync(int caixaId, CancellationToken ct = default);

    // VendaPdv
    Task<VendaPdvResponse> FinalizarVendaAsync(VendaPdvRequest request, CancellationToken ct = default);
    Task<VendaPdvResponse> CancelarVendaAsync(int id, CancellationToken ct = default);
    Task<VendaPdvResponse?> ObterVendaAsync(int id, CancellationToken ct = default);
    Task<List<VendaPdvResponse>> ListarVendasAsync(int? caixaId = null, CancellationToken ct = default);
}

public class PdvService : IPdvService
{
    private readonly IAppDbContext _db;
    private readonly IEstoqueService _estoque;

    public PdvService(IAppDbContext db, IEstoqueService estoque)
    {
        _db = db;
        _estoque = estoque;
    }

    // ── Caixa ─────────────────────────────────────────────────────────────────

    public async Task<CaixaResponse> AbrirCaixaAsync(AbrirCaixaRequest request, CancellationToken ct = default)
    {
        _ = await _db.Usuarios.FindAsync([request.OperadorId], ct)
            ?? throw new InvalidOperationException($"Operador {request.OperadorId} não encontrado.");

        var caixaAberto = await _db.Caixas
            .AnyAsync(c => c.OperadorId == request.OperadorId && c.Status == StatusCaixa.Aberto, ct);
        if (caixaAberto)
            throw new InvalidOperationException("Operador já possui um caixa aberto.");

        var caixa = new Caixa
        {
            OperadorId = request.OperadorId,
            SaldoInicial = request.SaldoInicial,
            Observacoes = request.Observacoes
        };

        caixa.Movimentos.Add(new MovimentoCaixa
        {
            Tipo = TipoMovimentoCaixa.Abertura,
            Valor = request.SaldoInicial,
            Descricao = "Abertura de caixa",
            OperadorId = request.OperadorId
        });

        _db.Caixas.Add(caixa);
        await _db.SaveChangesAsync(ct);

        return await ObterCaixaAsync(caixa.Id, ct)
            ?? throw new InvalidOperationException("Falha ao recuperar caixa recém-aberto.");
    }

    public async Task<CaixaResponse> FecharCaixaAsync(int caixaId, FecharCaixaRequest request, CancellationToken ct = default)
    {
        var caixa = await _db.Caixas
            .Include(c => c.Operador)
            .Include(c => c.Movimentos)
            .Include(c => c.Vendas).ThenInclude(v => v.Pagamentos)
            .FirstOrDefaultAsync(c => c.Id == caixaId, ct)
            ?? throw new InvalidOperationException($"Caixa {caixaId} não encontrado.");

        if (caixa.Status == StatusCaixa.Fechado)
            throw new InvalidOperationException("Caixa já está fechado.");

        var saldoCalculado = CalcularSaldoFinal(caixa);

        caixa.Status = StatusCaixa.Fechado;
        caixa.DataFechamento = DateTime.UtcNow;
        caixa.SaldoFinal = saldoCalculado;
        if (request.Observacoes is not null)
            caixa.Observacoes = request.Observacoes;

        caixa.Movimentos.Add(new MovimentoCaixa
        {
            CaixaId = caixaId,
            Tipo = TipoMovimentoCaixa.Fechamento,
            Valor = saldoCalculado,
            Descricao = "Fechamento de caixa",
            OperadorId = caixa.OperadorId
        });

        await _db.SaveChangesAsync(ct);

        return MapCaixa(caixa);
    }

    public async Task<CaixaResponse?> ObterCaixaAsync(int id, CancellationToken ct = default)
    {
        var caixa = await _db.Caixas
            .Include(c => c.Operador)
            .Include(c => c.Movimentos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return caixa is null ? null : MapCaixa(caixa);
    }

    public async Task<List<CaixaResponse>> ListarCaixasAsync(CancellationToken ct = default)
    {
        var lista = await _db.Caixas
            .Include(c => c.Operador)
            .Include(c => c.Movimentos)
            .AsNoTracking()
            .OrderByDescending(c => c.DataAbertura)
            .ToListAsync(ct);

        return lista.Select(MapCaixa).ToList();
    }

    public async Task<MovimentoCaixaResponse> SangriaAsync(int caixaId, MovimentoCaixaRequest request, CancellationToken ct = default)
        => await RegistrarMovimentoManualAsync(caixaId, TipoMovimentoCaixa.Sangria, request, ct);

    public async Task<MovimentoCaixaResponse> SuprimentoAsync(int caixaId, MovimentoCaixaRequest request, CancellationToken ct = default)
        => await RegistrarMovimentoManualAsync(caixaId, TipoMovimentoCaixa.Suprimento, request, ct);

    private async Task<MovimentoCaixaResponse> RegistrarMovimentoManualAsync(
        int caixaId, TipoMovimentoCaixa tipo, MovimentoCaixaRequest request, CancellationToken ct)
    {
        var caixa = await _db.Caixas.FindAsync([caixaId], ct)
            ?? throw new InvalidOperationException($"Caixa {caixaId} não encontrado.");

        if (caixa.Status == StatusCaixa.Fechado)
            throw new InvalidOperationException("Caixa está fechado.");

        var movimento = new MovimentoCaixa
        {
            CaixaId = caixaId,
            Tipo = tipo,
            Valor = request.Valor,
            Descricao = request.Descricao,
            OperadorId = request.OperadorId
        };

        _db.MovimentosCaixa.Add(movimento);
        await _db.SaveChangesAsync(ct);

        return MapMovimento(movimento);
    }

    public async Task<List<MovimentoCaixaResponse>> ListarMovimentosAsync(int caixaId, CancellationToken ct = default)
    {
        var movimentos = await _db.MovimentosCaixa
            .Where(m => m.CaixaId == caixaId)
            .AsNoTracking()
            .OrderBy(m => m.CriadoEm)
            .ToListAsync(ct);

        return movimentos.Select(MapMovimento).ToList();
    }

    // ── VendaPdv ──────────────────────────────────────────────────────────────

    public async Task<VendaPdvResponse> FinalizarVendaAsync(VendaPdvRequest request, CancellationToken ct = default)
    {
        var caixa = await _db.Caixas.FindAsync([request.CaixaId], ct)
            ?? throw new InvalidOperationException($"Caixa {request.CaixaId} não encontrado.");

        if (caixa.Status == StatusCaixa.Fechado)
            throw new InvalidOperationException("Caixa está fechado. Abra um novo caixa para registrar vendas.");

        _ = await _db.Usuarios.FindAsync([request.OperadorId], ct)
            ?? throw new InvalidOperationException($"Operador {request.OperadorId} não encontrado.");

        if (request.ClienteId.HasValue)
            _ = await _db.Clientes.FindAsync([request.ClienteId.Value], ct)
                ?? throw new InvalidOperationException($"Cliente {request.ClienteId} não encontrado.");

        if (request.Itens is null || request.Itens.Count == 0)
            throw new InvalidOperationException("Venda precisa de ao menos um item.");

        var itensAgrupados = request.Itens
            .GroupBy(i => i.ProdutoId)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Quantidade = g.Sum(x => x.Quantidade),
                Desconto = g.Sum(x => x.Desconto)
            }).ToList();

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

        var itensPdv = itensAgrupados.Select(item =>
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            return new ItemPdv
            {
                ProdutoId = produto.Id,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.PrecoVenda,
                Desconto = item.Desconto
            };
        }).ToList();

        var subtotal = itensPdv.Sum(i => i.PrecoUnitario * i.Quantidade);
        var descontoGlobal = Math.Min(request.Desconto, subtotal);
        var descontoItens = itensPdv.Sum(i => i.Desconto);
        var total = subtotal - descontoGlobal - descontoItens;

        if (total < 0)
            throw new InvalidOperationException("O total da venda não pode ser negativo.");

        var totalPago = request.Pagamentos.Sum(p => p.Valor);
        if (totalPago < total)
            throw new InvalidOperationException(
                $"Total pago ({totalPago:C}) é insuficiente para cobrir o total da venda ({total:C}).");

        var troco = totalPago - total;

        var pagamentos = request.Pagamentos.Select(p => new PagamentoPdv
        {
            Forma = p.Forma,
            Valor = p.Valor,
            Troco = p.Forma == FormaPagamento.Dinheiro ? Math.Max(0, troco) : 0
        }).ToList();

        var venda = new VendaPdv
        {
            CaixaId = request.CaixaId,
            OperadorId = request.OperadorId,
            ClienteId = request.ClienteId,
            Subtotal = subtotal,
            Desconto = descontoGlobal + descontoItens,
            Total = total,
            TotalPago = totalPago,
            Troco = troco,
            Observacoes = request.Observacoes,
            Itens = itensPdv,
            Pagamentos = pagamentos
        };

        _db.VendasPdv.Add(venda);
        await _db.SaveChangesAsync(ct);

        var valorDinheiro = pagamentos
            .Where(p => p.Forma == FormaPagamento.Dinheiro)
            .Sum(p => p.Valor - p.Troco);
        if (valorDinheiro > 0)
        {
            _db.MovimentosCaixa.Add(new MovimentoCaixa
            {
                CaixaId = request.CaixaId,
                Tipo = TipoMovimentoCaixa.Pagamento,
                Valor = valorDinheiro,
                Descricao = $"Venda PDV #{venda.Id}",
                OperadorId = request.OperadorId
            });
        }

        foreach (var item in itensPdv)
        {
            await _estoque.RegistrarMovimentacaoInternaAsync(
                item.ProdutoId, TipoMovimentacao.Saida, item.Quantidade,
                $"Venda PDV #{venda.Id}", new MovimentacaoContexto(VendaPdvId: venda.Id), ct);
        }

        await _db.SaveChangesAsync(ct);

        return await ObterVendaAsync(venda.Id, ct)
            ?? throw new InvalidOperationException("Falha ao recuperar venda recém-criada.");
    }

    public async Task<VendaPdvResponse> CancelarVendaAsync(int id, CancellationToken ct = default)
    {
        var venda = await _db.VendasPdv
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos)
            .Include(v => v.Operador)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new InvalidOperationException($"Venda PDV {id} não encontrada.");

        if (venda.Status == StatusVendaPdv.Cancelada)
            throw new InvalidOperationException("Venda já cancelada.");

        var caixa = await _db.Caixas.FindAsync([venda.CaixaId], ct)!;
        if (caixa!.Status == StatusCaixa.Fechado)
            throw new InvalidOperationException("Não é possível cancelar uma venda de caixa fechado.");

        venda.Status = StatusVendaPdv.Cancelada;

        foreach (var item in venda.Itens)
        {
            await _estoque.RegistrarMovimentacaoInternaAsync(
                item.ProdutoId, TipoMovimentacao.Entrada, item.Quantidade,
                $"Cancelamento da venda PDV #{id}", new MovimentacaoContexto(VendaPdvId: id), ct);
        }

        await _db.SaveChangesAsync(ct);

        return MapVenda(venda);
    }

    public async Task<VendaPdvResponse?> ObterVendaAsync(int id, CancellationToken ct = default)
    {
        var venda = await _db.VendasPdv
            .Include(v => v.Operador)
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        return venda is null ? null : MapVenda(venda);
    }

    public async Task<List<VendaPdvResponse>> ListarVendasAsync(int? caixaId = null, CancellationToken ct = default)
    {
        var query = _db.VendasPdv
            .Include(v => v.Operador)
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos)
            .AsNoTracking();

        if (caixaId.HasValue)
            query = query.Where(v => v.CaixaId == caixaId.Value);

        var lista = await query.OrderByDescending(v => v.DataVenda).ToListAsync(ct);
        return lista.Select(MapVenda).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static decimal CalcularSaldoFinal(Caixa caixa)
    {
        var saldo = caixa.SaldoInicial;
        foreach (var mov in caixa.Movimentos)
        {
            saldo += mov.Tipo switch
            {
                TipoMovimentoCaixa.Suprimento => mov.Valor,
                TipoMovimentoCaixa.Pagamento => mov.Valor,
                TipoMovimentoCaixa.Sangria => -mov.Valor,
                _ => 0
            };
        }
        return saldo;
    }

    private static CaixaResponse MapCaixa(Caixa c) => new(
        c.Id,
        c.OperadorId,
        c.Operador?.Nome ?? string.Empty,
        c.DataAbertura,
        c.DataFechamento,
        c.SaldoInicial,
        c.SaldoFinal,
        c.Status,
        c.Observacoes,
        c.Movimentos.OrderBy(m => m.CriadoEm).Select(MapMovimento).ToList());

    private static MovimentoCaixaResponse MapMovimento(MovimentoCaixa m) => new(
        m.Id,
        m.CaixaId,
        m.Tipo,
        m.Valor,
        m.Descricao,
        m.OperadorId,
        m.CriadoEm);

    private static VendaPdvResponse MapVenda(VendaPdv v) => new(
        v.Id,
        v.CaixaId,
        v.OperadorId,
        v.Operador?.Nome ?? string.Empty,
        v.ClienteId,
        v.Cliente?.Nome,
        v.Status,
        v.DataVenda,
        v.Subtotal,
        v.Desconto,
        v.Total,
        v.TotalPago,
        v.Troco,
        v.Observacoes,
        v.Itens.Select(i => new ItemPdvResponse(
            i.ProdutoId,
            i.Produto?.Nome ?? string.Empty,
            i.Quantidade,
            i.PrecoUnitario,
            i.Desconto,
            i.Subtotal)).ToList(),
        v.Pagamentos.Select(p => new PagamentoPdvResponse(p.Id, p.Forma, p.Valor, p.Troco)).ToList());
}
