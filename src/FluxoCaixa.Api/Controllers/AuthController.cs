using FluxoCaixa.Application;
using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(FluxoCaixaDbContext db, IPasswordHasher hasher, ITokenService tokenService, Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequestApi request, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (usuario is null || !_hasher.Verify(request.Password, usuario.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciais inválidas",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var accessToken = _tokenService.GerarToken(usuario);
        var refreshToken = _tokenService.GerarRefreshToken(usuario.Id, _jwtOptions.RefreshDays);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new LoginResponse(accessToken, refreshToken.Token, _jwtOptions.ExpMinutes * 60));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens.Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (token is null || token.Revogado || token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Refresh token inválido",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        token.Revogado = true;
        var novoRefresh = _tokenService.GerarRefreshToken(token.UsuarioId, _jwtOptions.RefreshDays);
        _db.RefreshTokens.Add(novoRefresh);
        var accessToken = _tokenService.GerarToken(token.Usuario!);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new LoginResponse(accessToken, novoRefresh.Token, _jwtOptions.ExpMinutes * 60));
    }
}
