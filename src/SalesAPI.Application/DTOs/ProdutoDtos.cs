using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record ProdutoRequest(
    [Required, MaxLength(150)] string Nome,
    [MaxLength(1000)] string? Descricao,
    [MaxLength(50)] string? CodigoInterno,
    [MaxLength(100)] string? SKU,
    [MaxLength(50)] string? CodigoBarras,
    int? CategoriaId,
    int? MarcaId,
    [MaxLength(20)] string Unidade,
    [Range(0, double.MaxValue)] decimal PrecoCusto,
    [Range(0, double.MaxValue)] decimal PrecoVenda,
    int EstoqueMinimo,
    StatusProduto Status,
    [MaxLength(500)] string? ImagemUrl,
    [MaxLength(10)] string? NCM,
    [MaxLength(10)] string? CFOP,
    [MaxLength(10)] string? CST,
    [MaxLength(2)] string? Origem,
    [Range(0, 100)] decimal Aliquota);

public record ProdutoResponse(
    int Id,
    string Nome,
    string Descricao,
    string? CodigoInterno,
    string? SKU,
    string? CodigoBarras,
    int? CategoriaId,
    string? CategoriaNome,
    int? MarcaId,
    string? MarcaNome,
    string Unidade,
    decimal PrecoCusto,
    decimal PrecoVenda,
    int EstoqueMinimo,
    StatusProduto Status,
    string? ImagemUrl,
    string? NCM,
    string? CFOP,
    string? CST,
    string? Origem,
    decimal Aliquota,
    int QuantidadeEstoque,
    DateTime CriadoEm);

public record ProdutoVariacaoRequest(
    [MaxLength(50)] string? Tamanho,
    [MaxLength(50)] string? Cor,
    [MaxLength(100)] string? Modelo,
    [MaxLength(100)] string? Material,
    [MaxLength(100)] string? SKU,
    [Range(0, double.MaxValue)] decimal Preco,
    int EstoqueQtd);

public record ProdutoVariacaoResponse(
    int Id,
    int ProdutoId,
    string? Tamanho,
    string? Cor,
    string? Modelo,
    string? Material,
    string? SKU,
    decimal Preco,
    int EstoqueQtd,
    bool Ativo);
