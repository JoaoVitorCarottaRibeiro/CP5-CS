namespace ExpenseHub.Api.Services;

/// <summary>
/// Desfecho de uma operacao de servico, convertido em status HTTP pelos endpoints.
/// </summary>
internal enum ServiceStatus
{
    /// <summary>Operacao concluida (200/201).</summary>
    Success,

    /// <summary>Entrada invalida (400).</summary>
    Invalid,

    /// <summary>Sem permissao para a operacao (403).</summary>
    Forbidden,

    /// <summary>Recurso inexistente ou fora do escopo de leitura (404).</summary>
    NotFound,

    /// <summary>Transicao incompativel ou repetida (409).</summary>
    Conflict,
}
