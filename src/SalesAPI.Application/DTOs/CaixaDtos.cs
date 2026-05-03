using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record AbrirCaixaRequest(
    [Required] int OperadorId,
    [Range(0, double.MaxValue)] decimal SaldoInicial,
    [MaxLength(500)] string? Observacoes);

public record FecharCaixaRequest(
    [MaxLength(500)] string? Observacoes);

public record MovimentoCaixaRequest(
    [Range(0.01, double.MaxValue)] decimal Valor,
    [MaxLength(300)] string? Descricao,
    int? OperadorId);

public record MovimentoCaixaResponse(
    int Id,
    int CaixaId,
    TipoMovimentoCaixa Tipo,
    decimal Valor,
    string? Descricao,
    int? OperadorId,
    DateTime CriadoEm);

public record CaixaResponse(
    int Id,
    int OperadorId,
    string OperadorNome,
    DateTime DataAbertura,
    DateTime? DataFechamento,
    decimal SaldoInicial,
    decimal? SaldoFinal,
    StatusCaixa Status,
    string? Observacoes,
    List<MovimentoCaixaResponse> Movimentos);

public record ItemPdvRequest(
    [Required] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade,
    [Range(0, double.MaxValue)] decimal Desconto = 0);

public record PagamentoPdvRequest(
    FormaPagamento Forma,
    [Range(0.01, double.MaxValue)] decimal Valor);

public record VendaPdvRequest(
    [Required] int CaixaId,
    [Required] int OperadorId,
    int? ClienteId,
    [Range(0, double.MaxValue)] decimal Desconto,
    [MaxLength(500)] string? Observacoes,
    [Required, MinLength(1)] List<ItemPdvRequest> Itens,
    [Required, MinLength(1)] List<PagamentoPdvRequest> Pagamentos);

public record ItemPdvResponse(
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Subtotal);

public record PagamentoPdvResponse(
    int Id,
    FormaPagamento Forma,
    decimal Valor,
    decimal Troco);

public record VendaPdvResponse(
    int Id,
    int CaixaId,
    int OperadorId,
    string OperadorNome,
    int? ClienteId,
    string? ClienteNome,
    StatusVendaPdv Status,
    DateTime DataVenda,
    decimal Subtotal,
    decimal Desconto,
    decimal Total,
    decimal TotalPago,
    decimal Troco,
    string? Observacoes,
    List<ItemPdvResponse> Itens,
    List<PagamentoPdvResponse> Pagamentos);
