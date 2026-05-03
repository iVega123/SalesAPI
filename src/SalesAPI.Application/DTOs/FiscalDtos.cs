using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record EmitirNfeRequest(
    TipoNotaFiscal Tipo,
    int? VendaId,
    int? VendaPdvId,
    int? ClienteId,
    [MaxLength(200)] string? NomeDestinatario,
    [MaxLength(18)] string? CpfCnpjDestinatario,
    [MaxLength(300)] string? EnderecoDestinatario,
    [MaxLength(3)] string Serie = "001");

public record ItemNotaFiscalResponse(
    int Id,
    int ProdutoId,
    string ProdutoNome,
    string Unidade,
    int Quantidade,
    decimal ValorUnitario,
    decimal ValorDesconto,
    decimal ValorBruto,
    string? NCM,
    string? CFOP,
    string? CST,
    decimal AliquotaICMS,
    decimal ValorICMS,
    decimal AliquotaPIS,
    decimal ValorPIS,
    decimal AliquotaCOFINS,
    decimal ValorCOFINS,
    decimal AliquotaIPI,
    decimal ValorIPI);

public record NotaFiscalResponse(
    int Id,
    TipoNotaFiscal Tipo,
    string? NumeroNota,
    string? Serie,
    string? ChaveAcesso,
    StatusNotaFiscal Status,
    int? VendaId,
    int? VendaPdvId,
    int? ClienteId,
    string? NomeDestinatario,
    string? CpfCnpjDestinatario,
    decimal ValorProdutos,
    decimal ValorFrete,
    decimal ValorDesconto,
    decimal ValorICMS,
    decimal ValorPIS,
    decimal ValorCOFINS,
    decimal ValorIPI,
    decimal ValorTotal,
    string? MensagemErro,
    DateTime? DataEmissao,
    DateTime? DataCancelamento,
    DateTime CriadoEm,
    List<ItemNotaFiscalResponse> Itens);

public record CancelarNfeRequest([Required, MinLength(15)] string Motivo);

public record CalcularImpostosRequest(
    [Required] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade,
    [Range(0, double.MaxValue)] decimal ValorUnitario,
    [Range(0, double.MaxValue)] decimal Desconto = 0);

public record CalcularImpostosResponse(
    decimal ValorBruto,
    decimal ValorDesconto,
    decimal BaseCalculo,
    decimal AliquotaICMS,
    decimal ValorICMS,
    decimal AliquotaPIS,
    decimal ValorPIS,
    decimal AliquotaCOFINS,
    decimal ValorCOFINS,
    decimal AliquotaIPI,
    decimal ValorIPI,
    decimal TotalImpostos,
    decimal ValorLiquido);
