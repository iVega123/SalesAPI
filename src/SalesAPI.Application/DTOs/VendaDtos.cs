using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record ItemVendaRequest(
    [Required] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade);

public record VendaRequest(
    [Required] int ClienteId,
    int? VendedorId,
    [Range(0, double.MaxValue)] decimal Desconto,
    [MaxLength(500)] string? Observacoes,
    [Range(1, 60)] int NumeroParcelas,
    DateTime PrimeiroVencimento,
    [Required, MinLength(1)] List<ItemVendaRequest> Itens);

public record ItemVendaResponse(int ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public record VendaResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    int? VendedorId,
    string? VendedorNome,
    StatusVenda Status,
    DateTime DataVenda,
    decimal Subtotal,
    decimal Desconto,
    decimal Total,
    string? Observacoes,
    List<ItemVendaResponse> Itens,
    List<ParcelaVendaResponse> Parcelas);

public record ParcelaVendaResponse(
    int Id,
    int Numero,
    int TotalParcelas,
    decimal Valor,
    DateTime Vencimento,
    DateTime? PagamentoEm,
    StatusParcela Status);

public record PagarParcelaRequest(DateTime? PagamentoEm);
