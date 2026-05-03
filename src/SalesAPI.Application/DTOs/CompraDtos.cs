using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record ItemCompraRequest(
    [Required] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade,
    [Range(0, double.MaxValue)] decimal PrecoUnitario);

public record CompraRequest(
    [Required] int FornecedorId,
    [Required] int UsuarioId,
    DateTime? DataPrevisaoEntrega,
    [Range(0, double.MaxValue)] decimal Frete,
    [Range(0, double.MaxValue)] decimal Impostos,
    [MaxLength(100)] string? NumeroNF,
    [MaxLength(500)] string? Observacoes,
    [Required, MinLength(1)] List<ItemCompraRequest> Itens);

public record ItemCompraResponse(
    int Id,
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    int QuantidadeRecebida,
    decimal PrecoUnitario,
    decimal Subtotal);

public record CompraResponse(
    int Id,
    int FornecedorId,
    string FornecedorNome,
    StatusCompra Status,
    DateTime DataCompra,
    DateTime? DataPrevisaoEntrega,
    decimal Subtotal,
    decimal Frete,
    decimal Impostos,
    decimal Total,
    string? NumeroNF,
    string? Observacoes,
    List<ItemCompraResponse> Itens,
    List<ContaPagarResponse> ContasPagar,
    DateTime CriadoEm);

public record ReceberCompraRequest(
    [Required] int UsuarioId,
    [MaxLength(500)] string? Observacoes,
    [Required, MinLength(1)] List<ItemRecebimentoRequest> Itens);

public record ItemRecebimentoRequest(
    [Required] int ItemCompraId,
    [Range(1, int.MaxValue)] int QuantidadeRecebida);

public record GerarParcelasCompraRequest(
    [Range(1, 60)] int NumeroParcelas,
    DateTime PrimeiroVencimento);

public record ContaPagarResponse(
    int Id,
    int Parcela,
    int TotalParcelas,
    decimal Valor,
    DateTime Vencimento,
    DateTime? PagamentoEm,
    StatusContaPagar Status);
