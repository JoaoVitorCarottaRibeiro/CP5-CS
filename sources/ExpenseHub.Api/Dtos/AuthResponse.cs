using System;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Resposta de autenticacao com o token de acesso e sua expiracao.
/// </summary>
internal sealed class AuthResponse
{
    /// <summary>Token JWT assinado.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Instante de expiracao do token em UTC.</summary>
    public DateTime ExpiresAtUtc { get; init; }
}
