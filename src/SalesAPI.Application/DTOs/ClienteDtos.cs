using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record ClienteRequest(
    [Required, MaxLength(150)] string Nome,
    TipoPessoa TipoPessoa,
    [MaxLength(14)] string? CPF,
    [MaxLength(18)] string? CNPJ,
    [Required, EmailAddress, MaxLength(150)] string Email,
    [MaxLength(20)] string? Telefone,
    DateTime? DataNascimento,
    [Range(0, double.MaxValue)] decimal LimiteCredito,
    [MaxLength(500)] string? Observacoes);

public record ClienteResponse(
    int Id,
    string Nome,
    TipoPessoa TipoPessoa,
    string? CPF,
    string? CNPJ,
    string Email,
    string? Telefone,
    DateTime? DataNascimento,
    decimal LimiteCredito,
    int PontosFidelidade,
    StatusCliente Status,
    DateTime CriadoEm);

public record EnderecoClienteRequest(
    [Required, MaxLength(200)] string Logradouro,
    [MaxLength(20)] string Numero,
    [MaxLength(100)] string? Complemento,
    [Required, MaxLength(100)] string Bairro,
    [Required, MaxLength(100)] string Cidade,
    [Required, MaxLength(2)] string Estado,
    [Required, MaxLength(9)] string CEP,
    TipoEndereco Tipo,
    bool Principal);

public record EnderecoClienteResponse(
    int Id,
    int ClienteId,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string CEP,
    TipoEndereco Tipo,
    bool Principal);
