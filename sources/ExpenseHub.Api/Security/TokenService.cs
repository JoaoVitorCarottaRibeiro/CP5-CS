using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseHub.Api.Configuration;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Gera tokens JWT assinados a partir do usuario autenticado e das suas roles.
/// As roles vao como claims para que a autorizacao por role funcione no servidor.
/// </summary>
internal sealed class TokenService
{
    private readonly JwtOptions _options;

    /// <summary>Cria o servico com as opcoes de JWT.</summary>
    /// <param name="options">Opcoes de JWT.</param>
    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Cria um token para o usuario com as roles informadas.</summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="roles">Roles atuais do usuario.</param>
    /// <returns>Token assinado e instante de expiracao em UTC.</returns>
    public AuthResponse CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.Key));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        string encoded = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse
        {
            AccessToken = encoded,
            ExpiresAtUtc = expiresAt,
        };
    }
}
