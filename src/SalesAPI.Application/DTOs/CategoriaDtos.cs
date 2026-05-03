using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Application.DTOs;

public record CategoriaRequest(
    [Required, MaxLength(100)] string Nome,
    [MaxLength(300)] string? Descricao,
    bool Ativo = true);

public record CategoriaResponse(int Id, string Nome, string Descricao, bool Ativo, DateTime CriadoEm);

public record MarcaRequest(
    [Required, MaxLength(100)] string Nome,
    bool Ativo = true);

public record MarcaResponse(int Id, string Nome, bool Ativo, DateTime CriadoEm);
