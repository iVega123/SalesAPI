using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Application.DTOs;

public record UsuarioResponse(
    int Id,
    string Nome,
    string Email,
    int PerfilId,
    string PerfilNome,
    bool Ativo,
    DateTime? UltimoAcesso,
    DateTime CriadoEm);

public record AtualizarUsuarioRequest(
    [Required, MaxLength(150)] string Nome,
    [Required] int PerfilId,
    bool Ativo);
