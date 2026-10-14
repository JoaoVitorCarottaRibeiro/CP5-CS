namespace ExpenseHub.Api.Services;

/// <summary>
/// Operacoes sobre um reembolso existente avaliadas pela <see cref="ExpensePolicy"/>.
/// </summary>
internal enum ExpenseOperation
{
    /// <summary>Consultar detalhe ou historico.</summary>
    Read,

    /// <summary>Editar o rascunho.</summary>
    Edit,

    /// <summary>Enviar o rascunho para analise.</summary>
    Submit,

    /// <summary>Aprovar um reembolso enviado.</summary>
    Approve,

    /// <summary>Reprovar um reembolso enviado.</summary>
    Reject,

    /// <summary>Registrar o pagamento de um reembolso aprovado.</summary>
    Pay,
}
