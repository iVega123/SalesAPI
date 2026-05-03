using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Application.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Senha);

public record LoginResponse(string Token, string RefreshToken, string Nome, string Perfil, DateTime Expiracao);

public record RegistrarUsuarioRequest(
    [Required, MaxLength(150)] string Nome,
    [Required, EmailAddress, MaxLength(150)] string Email,
    [Required, MinLength(6)] string Senha,
    [Required] int PerfilId);

public record RefreshTokenRequest([Required] string Token);
public record RefreshTokenResponse(string Token, string RefreshToken, DateTime Expiracao);
