namespace ExpenseHub.Api.Services;

/// <summary>
/// Resultado da avaliacao de uma operacao. Cada valor corresponde a um status
/// HTTP do contrato.
/// </summary>
internal enum AccessDecision
{
    /// <summary>Operacao permitida.</summary>
    Allowed,

    /// <summary>Recurso inexistente ou fora do escopo de leitura (404).</summary>
    NotFound,

    /// <summary>Autenticado, mas sem permissao para a operacao (403).</summary>
    Forbidden,

    /// <summary>Estado atual incompativel com a transicao (409).</summary>
    Conflict,
}
