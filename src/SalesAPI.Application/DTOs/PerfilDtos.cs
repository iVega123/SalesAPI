using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record PerfilRequest(
    [Required, MaxLength(100)] string Nome,
    [MaxLength(300)] string? Descricao,
    bool Ativo = true);

public record PerfilResponse(int Id, string Nome, string Descricao, bool Ativo);

public record PermissaoRequest(
    [Required] int PerfilId,
    [Required, MaxLength(100)] string Recurso,
    [Required] AcaoPermissao Acao);

public record PermissaoResponse(int Id, int PerfilId, string Recurso, AcaoPermissao Acao);
