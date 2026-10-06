using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro de pagamento de um reembolso aprovado. Ha no maximo um pagamento
/// por reembolso. Ator e horario sao definidos pelo servidor.
/// </summary>
internal sealed class PaymentRecord
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public int Id { get; set; }

    /// <summary>Reembolso pago.</summary>
    public int ExpenseId { get; set; }

    /// <summary>Reembolso pago.</summary>
    public Expense? Expense { get; set; }

    /// <summary>Identificador do usuario Finance que registrou o pagamento.</summary>
    public string PaidById { get; set; } = string.Empty;

    /// <summary>Usuario que registrou o pagamento.</summary>
    public ApplicationUser? PaidBy { get; set; }

    /// <summary>Instante do pagamento em UTC.</summary>
    public DateTime PaidAtUtc { get; set; }

    /// <summary>Valor pago.</summary>
    public decimal Amount { get; set; }
}
