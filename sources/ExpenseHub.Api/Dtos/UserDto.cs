using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Representacao de um usuario para as rotas administrativas.
/// </summary>
internal sealed class UserDto
{
    /// <summary>Identificador do usuario.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>E-mail do usuario.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Roles atribuidas ao usuario.</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
