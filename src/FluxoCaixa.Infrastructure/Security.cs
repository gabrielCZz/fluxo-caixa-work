using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluxoCaixa.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FluxoCaixa.Infrastructure;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpMinutes { get; set; }
    public int RefreshDays { get; set; }
}

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verify(string senha, string hash);
}

public sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(key);
    }

    public bool Verify(string senha, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var key = Convert.FromBase64String(parts[1]);
        var computed = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, HashAlgorithmName.SHA256, key.Length);
        return CryptographicOperations.FixedTimeEquals(key, computed);
    }
}

public interface ITokenService
{
    string GerarToken(Usuario usuario);
    RefreshToken GerarRefreshToken(Guid usuarioId, int dias);
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email)
        };

        var roles = usuario.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GerarRefreshToken(Guid usuarioId, int dias)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(dias),
            Revogado = false
        };
    }
}
