namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estados possiveis de um reembolso. A transicao entre estados e controlada
/// exclusivamente pelo servidor, conforme a maquina de estados do contrato.
/// </summary>
internal enum ExpenseState
{
    /// <summary>Rascunho editavel pelo proprietario.</summary>
    Draft,

    /// <summary>Enviado para analise, aguardando aprovacao ou reprovacao.</summary>
    Submitted,

    /// <summary>Aprovado, aguardando pagamento.</summary>
    Approved,

    /// <summary>Reprovado. Estado final.</summary>
    Rejected,

    /// <summary>Pago. Estado final.</summary>
    Paid,
}
