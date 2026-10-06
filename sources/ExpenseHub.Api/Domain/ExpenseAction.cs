namespace ExpenseHub.Api.Domain;

/// <summary>
/// Acoes registradas no historico de um reembolso.
/// </summary>
internal enum ExpenseAction
{
    /// <summary>Criacao do rascunho.</summary>
    Created,

    /// <summary>Edicao de um rascunho.</summary>
    Updated,

    /// <summary>Envio para analise.</summary>
    Submitted,

    /// <summary>Aprovacao.</summary>
    Approved,

    /// <summary>Reprovacao com justificativa.</summary>
    Rejected,

    /// <summary>Registro de pagamento.</summary>
    Paid,
}
