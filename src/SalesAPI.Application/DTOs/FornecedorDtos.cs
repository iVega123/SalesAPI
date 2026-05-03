using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record FornecedorRequest(
    [Required, MaxLength(200)] string RazaoSocial,
    [MaxLength(200)] string? NomeFantasia,
    [Required, MaxLength(18)] string CNPJ,
    [MaxLength(30)] string? IE,
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(20)] string? Telefone,
    [MaxLength(200)] string? Site,
    [MaxLength(300)] string? CondicoesPagamento,
    [MaxLength(500)] string? Observacoes);

public record FornecedorResponse(
    int Id,
    string RazaoSocial,
    string NomeFantasia,
    string CNPJ,
    string? IE,
    string? Email,
    string? Telefone,
    string? Site,
    string? CondicoesPagamento,
    StatusFornecedor Status,
    DateTime CriadoEm);

public record ContatoFornecedorRequest(
    [Required, MaxLength(150)] string Nome,
    [MaxLength(100)] string? Cargo,
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(20)] string? Telefone,
    bool Principal);

public record ContatoFornecedorResponse(
    int Id,
    int FornecedorId,
    string Nome,
    string? Cargo,
    string? Email,
    string? Telefone,
    bool Principal);
