using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface IFinanceiroService
{
    // Contas Bancárias
    Task<List<ContaBancariaResponse>> ListarContasBancariasAsync(CancellationToken ct = default);
    Task<ContaBancariaResponse?> ObterContaBancariaAsync(int id, CancellationToken ct = default);
    Task<ContaBancariaResponse> CriarContaBancariaAsync(ContaBancariaRequest request, CancellationToken ct = default);

    // Contas a Receber
    Task<List<ContaReceberResponse>> ListarContasReceberAsync(CancellationToken ct = default);
    Task<ContaReceberResponse?> ObterContaReceberAsync(int id, CancellationToken ct = default);
    Task<ContaReceberResponse> CriarContaReceberAsync(ContaReceberRequest request, CancellationToken ct = default);
    Task<ContaReceberResponse> ReceberContaAsync(int id, ReceberContaRequest request, CancellationToken ct = default);
    Task<ContaReceberResponse> CancelarContaReceberAsync(int id, CancellationToken ct = default);

    // Contas a Pagar
    Task<List<ContaPagarResponse>> ListarContasPagarAsync(CancellationToken ct = default);
    Task<ContaPagarResponse> PagarContaAsync(int id, PagarContaRequest request, CancellationToken ct = default);

    // Lançamentos Manuais
    Task<LancamentoFinanceiroResponse> CriarLancamentoAsync(LancamentoFinanceiroRequest request, CancellationToken ct = default);
    Task<List<LancamentoFinanceiroResponse>> ListarLancamentosAsync(CancellationToken ct = default);

    // Relatórios
    Task<FluxoCaixaResponse> ObterFluxoCaixaAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<DreResponse> ObterDreAsync(int ano, int mes, CancellationToken ct = default);
}

public class FinanceiroService : IFinanceiroService
{
    private readonly IAppDbContext _db;

    public FinanceiroService(IAppDbContext db) => _db = db;

    // ── Contas Bancárias ──────────────────────────────────────────────────────

    public async Task<List<ContaBancariaResponse>> ListarContasBancariasAsync(CancellationToken ct = default)
    {
        var lista = await _db.ContasBancarias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync(ct);
        return lista.Select(MapContaBancaria).ToList();
    }

    public async Task<ContaBancariaResponse?> ObterContaBancariaAsync(int id, CancellationToken ct = default)
    {
        var conta = await _db.ContasBancarias.FindAsync([id], ct);
        return conta is null ? null : MapContaBancaria(conta);
    }

    public async Task<ContaBancariaResponse> CriarContaBancariaAsync(ContaBancariaRequest request, CancellationToken ct = default)
    {
        var conta = new ContaBancaria
        {
            Nome = request.Nome,
            Banco = request.Banco,
            Agencia = request.Agencia,
            Conta = request.Conta,
            Tipo = request.Tipo,
            SaldoInicial = request.SaldoInicial
        };
        _db.ContasBancarias.Add(conta);
        await _db.SaveChangesAsync(ct);
        return MapContaBancaria(conta);
    }

    // ── Contas a Receber ──────────────────────────────────────────────────────

    public async Task<List<ContaReceberResponse>> ListarContasReceberAsync(CancellationToken ct = default)
    {
        var lista = await _db.ContasReceber
            .Include(c => c.Cliente)
            .AsNoTracking()
            .OrderBy(c => c.Vencimento)
            .ToListAsync(ct);
        return lista.Select(MapContaReceber).ToList();
    }

    public async Task<ContaReceberResponse?> ObterContaReceberAsync(int id, CancellationToken ct = default)
    {
        var conta = await _db.ContasReceber
            .Include(c => c.Cliente)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        return conta is null ? null : MapContaReceber(conta);
    }

    public async Task<ContaReceberResponse> CriarContaReceberAsync(ContaReceberRequest request, CancellationToken ct = default)
    {
        if (request.ClienteId.HasValue)
            _ = await _db.Clientes.FindAsync([request.ClienteId.Value], ct)
                ?? throw new InvalidOperationException($"Cliente {request.ClienteId} não encontrado.");

        var conta = new ContaReceber
        {
            Descricao = request.Descricao,
            ClienteId = request.ClienteId,
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            Observacoes = request.Observacoes
        };
        _db.ContasReceber.Add(conta);
        await _db.SaveChangesAsync(ct);
        return MapContaReceber(conta);
    }

    public async Task<ContaReceberResponse> ReceberContaAsync(int id, ReceberContaRequest request, CancellationToken ct = default)
    {
        var conta = await _db.ContasReceber
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"Conta a receber {id} não encontrada.");

        if (conta.Status == StatusContaReceber.Paga)
            throw new InvalidOperationException("Conta já recebida.");

        if (conta.Status == StatusContaReceber.Cancelada)
            throw new InvalidOperationException("Conta cancelada não pode ser recebida.");

        conta.Status = StatusContaReceber.Paga;
        conta.PagamentoEm = request.PagamentoEm ?? DateTime.UtcNow;

        _db.LancamentosFinanceiros.Add(new LancamentoFinanceiro
        {
            Descricao = conta.Descricao,
            Tipo = TipoLancamento.Receita,
            Valor = conta.Valor,
            DataLancamento = conta.PagamentoEm.Value,
            Categoria = "Recebimento",
            ContaReceberId = conta.Id
        });

        await _db.SaveChangesAsync(ct);
        return MapContaReceber(conta);
    }

    public async Task<ContaReceberResponse> CancelarContaReceberAsync(int id, CancellationToken ct = default)
    {
        var conta = await _db.ContasReceber
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"Conta a receber {id} não encontrada.");

        if (conta.Status == StatusContaReceber.Cancelada)
            throw new InvalidOperationException("Conta já cancelada.");

        conta.Status = StatusContaReceber.Cancelada;
        await _db.SaveChangesAsync(ct);
        return MapContaReceber(conta);
    }

    // ── Contas a Pagar ────────────────────────────────────────────────────────

    public async Task<List<ContaPagarResponse>> ListarContasPagarAsync(CancellationToken ct = default)
    {
        var lista = await _db.ContasPagar
            .AsNoTracking()
            .OrderBy(c => c.Vencimento)
            .ToListAsync(ct);
        return lista.Select(MapContaPagar).ToList();
    }

    public async Task<ContaPagarResponse> PagarContaAsync(int id, PagarContaRequest request, CancellationToken ct = default)
    {
        var conta = await _db.ContasPagar
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"Conta a pagar {id} não encontrada.");

        if (conta.Status == StatusContaPagar.Paga)
            throw new InvalidOperationException("Conta já paga.");

        if (conta.Status == StatusContaPagar.Cancelada)
            throw new InvalidOperationException("Conta cancelada não pode ser paga.");

        conta.Status = StatusContaPagar.Paga;
        conta.PagamentoEm = request.PagamentoEm ?? DateTime.UtcNow;

        _db.LancamentosFinanceiros.Add(new LancamentoFinanceiro
        {
            Descricao = $"Pagamento conta #{conta.Id} — parcela {conta.Parcela}/{conta.TotalParcelas}",
            Tipo = TipoLancamento.Despesa,
            Valor = conta.Valor,
            DataLancamento = conta.PagamentoEm.Value,
            Categoria = "Pagamento a fornecedor",
            ContaPagarId = conta.Id
        });

        await _db.SaveChangesAsync(ct);
        return MapContaPagar(conta);
    }

    // ── Lançamentos Manuais ───────────────────────────────────────────────────

    public async Task<LancamentoFinanceiroResponse> CriarLancamentoAsync(LancamentoFinanceiroRequest request, CancellationToken ct = default)
    {
        if (request.ContaBancariaId.HasValue)
            _ = await _db.ContasBancarias.FindAsync([request.ContaBancariaId.Value], ct)
                ?? throw new InvalidOperationException($"Conta bancária {request.ContaBancariaId} não encontrada.");

        var lancamento = new LancamentoFinanceiro
        {
            Descricao = request.Descricao,
            Tipo = request.Tipo,
            Valor = request.Valor,
            DataLancamento = request.DataLancamento,
            Categoria = request.Categoria,
            ContaBancariaId = request.ContaBancariaId
        };
        _db.LancamentosFinanceiros.Add(lancamento);
        await _db.SaveChangesAsync(ct);
        return MapLancamento(lancamento);
    }

    public async Task<List<LancamentoFinanceiroResponse>> ListarLancamentosAsync(CancellationToken ct = default)
    {
        var lista = await _db.LancamentosFinanceiros
            .AsNoTracking()
            .OrderByDescending(l => l.DataLancamento)
            .ToListAsync(ct);
        return lista.Select(MapLancamento).ToList();
    }

    // ── Relatórios ────────────────────────────────────────────────────────────

    public async Task<FluxoCaixaResponse> ObterFluxoCaixaAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        fim = fim.Date.AddDays(1).AddTicks(-1);

        var parcelasPagas = await _db.ParcelasVenda
            .Where(p => p.Status == StatusParcela.Paga && p.PagamentoEm >= inicio && p.PagamentoEm <= fim)
            .Select(p => new { Data = p.PagamentoEm!.Value.Date, Valor = p.Valor })
            .ToListAsync(ct);

        var pagamentosPdvImediatos = await _db.PagamentosPdv
            .Include(p => p.VendaPdv)
            .Where(p => p.VendaPdv!.Status == StatusVendaPdv.Finalizada
                     && (p.Forma == FormaPagamento.Dinheiro || p.Forma == FormaPagamento.CartaoDebito || p.Forma == FormaPagamento.Pix)
                     && p.VendaPdv.DataVenda >= inicio && p.VendaPdv.DataVenda <= fim)
            .Select(p => new { Data = p.VendaPdv!.DataVenda.Date, Valor = p.Valor - p.Troco })
            .ToListAsync(ct);

        var contasRecebidas = await _db.ContasReceber
            .Where(c => c.Status == StatusContaReceber.Paga && c.PagamentoEm >= inicio && c.PagamentoEm <= fim)
            .Select(c => new { Data = c.PagamentoEm!.Value.Date, Valor = c.Valor })
            .ToListAsync(ct);

        var lancamentosReceita = await _db.LancamentosFinanceiros
            .Where(l => l.Tipo == TipoLancamento.Receita && l.DataLancamento >= inicio && l.DataLancamento <= fim
                     && l.ContaReceberId == null)
            .Select(l => new { Data = l.DataLancamento.Date, Valor = l.Valor })
            .ToListAsync(ct);

        var contasPagas = await _db.ContasPagar
            .Where(c => c.Status == StatusContaPagar.Paga && c.PagamentoEm >= inicio && c.PagamentoEm <= fim)
            .Select(c => new { Data = c.PagamentoEm!.Value.Date, Valor = c.Valor })
            .ToListAsync(ct);

        var lancamentosDespesa = await _db.LancamentosFinanceiros
            .Where(l => l.Tipo == TipoLancamento.Despesa && l.DataLancamento >= inicio && l.DataLancamento <= fim
                     && l.ContaPagarId == null)
            .Select(l => new { Data = l.DataLancamento.Date, Valor = l.Valor })
            .ToListAsync(ct);

        var todasEntradas = parcelasPagas
            .Concat(pagamentosPdvImediatos)
            .Concat(contasRecebidas)
            .Concat(lancamentosReceita)
            .ToList();

        var todasSaidas = contasPagas
            .Concat(lancamentosDespesa)
            .ToList();

        var dias = new List<FluxoCaixaDiaResponse>();
        for (var d = inicio.Date; d <= fim.Date; d = d.AddDays(1))
        {
            var entradas = todasEntradas.Where(e => e.Data == d).Sum(e => e.Valor);
            var saidas = todasSaidas.Where(s => s.Data == d).Sum(s => s.Valor);
            if (entradas > 0 || saidas > 0)
                dias.Add(new FluxoCaixaDiaResponse(d, entradas, saidas, entradas - saidas));
        }

        return new FluxoCaixaResponse(
            inicio.Date,
            fim.Date,
            todasEntradas.Sum(e => e.Valor),
            todasSaidas.Sum(s => s.Valor),
            todasEntradas.Sum(e => e.Valor) - todasSaidas.Sum(s => s.Valor),
            dias);
    }

    public async Task<DreResponse> ObterDreAsync(int ano, int mes, CancellationToken ct = default)
    {
        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1).AddTicks(-1);

        var vendas = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Where(v => v.Status == StatusVenda.Confirmada && v.DataVenda >= inicio && v.DataVenda <= fim)
            .ToListAsync(ct);

        var vendasPdv = await _db.VendasPdv
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Where(v => v.Status == StatusVendaPdv.Finalizada && v.DataVenda >= inicio && v.DataVenda <= fim)
            .ToListAsync(ct);

        var receitaBrutaVendas = vendas.Sum(v => v.Total + v.Desconto);
        var receitaBrutaPdv = vendasPdv.Sum(v => v.Total + v.Desconto);
        var receitaBruta = receitaBrutaVendas + receitaBrutaPdv;

        var descontos = vendas.Sum(v => v.Desconto) + vendasPdv.Sum(v => v.Desconto);
        var receitaLiquida = receitaBruta - descontos;

        var cmvVendas = vendas.SelectMany(v => v.Itens)
            .Sum(i => (i.Produto?.PrecoCusto ?? 0) * i.Quantidade);
        var cmvPdv = vendasPdv.SelectMany(v => v.Itens)
            .Sum(i => (i.Produto?.PrecoCusto ?? 0) * i.Quantidade);
        var custoMercadorias = cmvVendas + cmvPdv;

        var lucroBruto = receitaLiquida - custoMercadorias;

        var despesasContasPagar = await _db.ContasPagar
            .Where(c => c.Status == StatusContaPagar.Paga && c.PagamentoEm >= inicio && c.PagamentoEm <= fim)
            .SumAsync(c => c.Valor, ct);

        var despesasLancamentos = await _db.LancamentosFinanceiros
            .Where(l => l.Tipo == TipoLancamento.Despesa && l.DataLancamento >= inicio && l.DataLancamento <= fim
                     && l.ContaPagarId == null)
            .SumAsync(l => l.Valor, ct);

        var despesasOperacionais = despesasContasPagar + despesasLancamentos;
        var resultadoLiquido = lucroBruto - despesasOperacionais;

        return new DreResponse(
            ano,
            mes,
            receitaBruta,
            descontos,
            receitaLiquida,
            custoMercadorias,
            lucroBruto,
            despesasOperacionais,
            resultadoLiquido);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static ContaBancariaResponse MapContaBancaria(ContaBancaria c) => new(
        c.Id, c.Nome, c.Banco, c.Agencia, c.Conta, c.Tipo, c.SaldoInicial, c.Ativo, c.CriadoEm);

    private static ContaReceberResponse MapContaReceber(ContaReceber c) => new(
        c.Id, c.Descricao, c.ClienteId, c.Cliente?.Nome, c.VendaId, c.VendaPdvId,
        c.Valor, c.Vencimento, c.PagamentoEm, c.Status, c.Observacoes);

    private static ContaPagarResponse MapContaPagar(ContaPagar c) => new(
        c.Id, c.Parcela, c.TotalParcelas, c.Valor, c.Vencimento, c.PagamentoEm, c.Status);

    private static LancamentoFinanceiroResponse MapLancamento(LancamentoFinanceiro l) => new(
        l.Id, l.Descricao, l.Tipo, l.Valor, l.DataLancamento, l.Categoria,
        l.ContaBancariaId, l.CompraId, l.VendaId, l.VendaPdvId, l.ContaPagarId, l.ContaReceberId, l.CriadoEm);
}
