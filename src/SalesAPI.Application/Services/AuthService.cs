using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UsuarioResponse> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken ct = default);
    Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(IAppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Ativo, ct)
            ?? throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        usuario.UltimoAcesso = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var token = GerarToken(usuario);
        var expiracao = DateTime.UtcNow.AddHours(GetExpiracaoHoras());
        var refreshToken = await SalvarRefreshTokenAsync(usuario.Id, ct);

        return new LoginResponse(token, refreshToken, usuario.Nome, usuario.Perfil?.Nome ?? string.Empty, expiracao);
    }

    public async Task<UsuarioResponse> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken ct = default)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Email == request.Email, ct))
            throw new InvalidOperationException($"E-mail '{request.Email}' já está em uso.");

        var perfil = await _db.Perfis.FindAsync([request.PerfilId], ct)
            ?? throw new InvalidOperationException($"Perfil {request.PerfilId} não encontrado.");

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Email = request.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            PerfilId = request.PerfilId
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);
        return MapUsuario(usuario, perfil.Nome);
    }

    public async Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var rt = await _db.RefreshTokens
            .Include(r => r.Usuario).ThenInclude(u => u!.Perfil)
            .FirstOrDefaultAsync(r => r.Token == request.Token && !r.Revogado, ct)
            ?? throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");

        if (rt.ExpiraEm < DateTime.UtcNow)
        {
            rt.Revogado = true;
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Refresh token expirado.");
        }

        if (rt.Usuario is null || !rt.Usuario.Ativo)
            throw new UnauthorizedAccessException("Usuário inativo.");

        rt.Revogado = true;

        var novoToken = GerarToken(rt.Usuario);
        var expiracao = DateTime.UtcNow.AddHours(GetExpiracaoHoras());
        var novoRt = await SalvarRefreshTokenAsync(rt.UsuarioId, ct);

        await _db.SaveChangesAsync(ct);
        return new RefreshTokenResponse(novoToken, novoRt, expiracao);
    }

    private string GerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim("perfil_id", usuario.PerfilId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetExpiracaoHoras()),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> SalvarRefreshTokenAsync(int usuarioId, CancellationToken ct)
    {
        var tokenValor = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = tokenValor,
            UsuarioId = usuarioId,
            ExpiraEm = DateTime.UtcNow.AddDays(GetRefreshExpiracaoDias())
        });
        await _db.SaveChangesAsync(ct);
        return tokenValor;
    }

    private string GetSecretKey() =>
        _config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey não configurada.");

    private double GetExpiracaoHoras() =>
        double.TryParse(_config["Jwt:ExpiracaoHoras"], out var h) ? h : 8;

    private double GetRefreshExpiracaoDias() =>
        double.TryParse(_config["Jwt:RefreshExpiracaoDias"], out var d) ? d : 30;

    private static UsuarioResponse MapUsuario(Usuario u, string perfilNome) =>
        new(u.Id, u.Nome, u.Email, u.PerfilId, perfilNome, u.Ativo, u.UltimoAcesso, u.CriadoEm);
}
