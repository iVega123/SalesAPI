using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record ContaBancariaRequest(
    [Required, MaxLength(150)] string Nome,
    [MaxLength(100)] string? Banco,
    [MaxLength(20)] string? Agencia,
    [MaxLength(30)] string? Conta,
    TipoConta Tipo,
    [Range(0, double.MaxValue)] decimal SaldoInicial);

public record ContaBancariaResponse(
    int Id,
    string Nome,
    string? Banco,
    string? Agencia,
    string? Conta,
    TipoConta Tipo,
    decimal SaldoInicial,
    bool Ativo,
    DateTime CriadoEm);

public record ContaReceberRequest(
    [Required, MaxLength(300)] string Descricao,
    int? ClienteId,
    [Range(0.01, double.MaxValue)] decimal Valor,
    DateTime Vencimento,
    [MaxLength(500)] string? Observacoes);

public record ReceberContaRequest(DateTime? PagamentoEm);

public record ContaReceberResponse(
    int Id,
    string Descricao,
    int? ClienteId,
    string? ClienteNome,
    int? VendaId,
    int? VendaPdvId,
    decimal Valor,
    DateTime Vencimento,
    DateTime? PagamentoEm,
    StatusContaReceber Status,
    string? Observacoes);

public record PagarContaRequest(DateTime? PagamentoEm);

public record LancamentoFinanceiroRequest(
    [Required, MaxLength(300)] string Descricao,
    TipoLancamento Tipo,
    [Range(0.01, double.MaxValue)] decimal Valor,
    DateTime DataLancamento,
    [MaxLength(100)] string? Categoria,
    int? ContaBancariaId);

public record LancamentoFinanceiroResponse(
    int Id,
    string Descricao,
    TipoLancamento Tipo,
    decimal Valor,
    DateTime DataLancamento,
    string? Categoria,
    int? ContaBancariaId,
    int? CompraId,
    int? VendaId,
    int? VendaPdvId,
    int? ContaPagarId,
    int? ContaReceberId,
    DateTime CriadoEm);

public record FluxoCaixaFiltro(DateTime Inicio, DateTime Fim);

public record FluxoCaixaDiaResponse(
    DateTime Data,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Saldo);

public record FluxoCaixaResponse(
    DateTime Inicio,
    DateTime Fim,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoLiquido,
    List<FluxoCaixaDiaResponse> Dias);

public record DreResponse(
    int Ano,
    int Mes,
    decimal ReceitaBruta,
    decimal Descontos,
    decimal ReceitaLiquida,
    decimal CustoMercadorias,
    decimal LucroBruto,
    decimal DespesasOperacionais,
    decimal ResultadoLiquido);
