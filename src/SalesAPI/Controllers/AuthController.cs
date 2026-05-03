using Microsoft.AspNetCore.Mvc;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Services;

namespace SalesAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(req, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }

    [HttpPost("registrar")]
    public async Task<ActionResult<UsuarioResponse>> Registrar([FromBody] RegistrarUsuarioRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _auth.RegistrarAsync(req, ct);
            return CreatedAtAction(nameof(Login), result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.RefreshAsync(req, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }
}
