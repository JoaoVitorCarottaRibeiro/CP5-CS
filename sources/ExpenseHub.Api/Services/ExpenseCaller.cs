using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Identidade de quem chama uma operacao de reembolso: o identificador e as
/// roles vem exclusivamente do token, nunca do corpo da requisicao.
/// </summary>
internal sealed class ExpenseCaller
{
    private readonly HashSet<string> _roles;

    /// <summary>Cria o chamador a partir do usuario autenticado.</summary>
    /// <param name="userId">Identificador do usuario autenticado.</param>
    /// <param name="roles">Roles presentes no token.</param>
    public ExpenseCaller(string userId, IEnumerable<string> roles)
    {
        UserId = userId;
        _roles = roles.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Identificador do usuario autenticado.</summary>
    public string UserId { get; }

    /// <summary>Possui a role Employee.</summary>
    public bool IsEmployee => _roles.Contains(Roles.Employee);

    /// <summary>Possui a role Approver.</summary>
    public bool IsApprover => _roles.Contains(Roles.Approver);

    /// <summary>Possui a role Finance.</summary>
    public bool IsFinance => _roles.Contains(Roles.Finance);

    /// <summary>Possui a role Auditor.</summary>
    public bool IsAuditor => _roles.Contains(Roles.Auditor);

    /// <summary>Indica se o chamador e o proprietario informado.</summary>
    /// <param name="ownerId">Proprietario do reembolso.</param>
    /// <returns><c>true</c> quando o chamador e o proprietario.</returns>
    public bool Owns(string ownerId)
    {
        return string.Equals(UserId, ownerId, StringComparison.Ordinal);
    }
}
