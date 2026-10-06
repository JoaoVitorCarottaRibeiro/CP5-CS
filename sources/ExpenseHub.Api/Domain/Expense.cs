using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitacao de reembolso. Possui um unico valor e segue a maquina de estados
/// do contrato. Proprietario, estado, atores e horarios sao definidos pelo servidor.
/// </summary>
internal sealed class Expense
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public int Id { get; set; }

    /// <summary>Identificador do proprietario, obtido do usuario autenticado.</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Proprietario do reembolso.</summary>
    public ApplicationUser? Owner { get; set; }

    /// <summary>Categoria da despesa.</summary>
    public int CategoryId { get; set; }

    /// <summary>Categoria associada.</summary>
    public ExpenseCategory? Category { get; set; }

    /// <summary>Descricao da despesa (10 a 500 caracteres).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Valor em reais, entre R$ 0,01 e Int32.MaxValue.</summary>
    public decimal Amount { get; set; }

    /// <summary>Data da despesa, valida e nao futura.</summary>
    public DateTime ExpenseDate { get; set; }

    /// <summary>Estado atual, controlado pelo servidor.</summary>
    public ExpenseState State { get; set; }

    /// <summary>Instante de criacao em UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Instante da ultima atualizacao em UTC.</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Historico de acoes do reembolso.</summary>
    public ICollection<ExpenseHistory> History { get; } = new List<ExpenseHistory>();

    /// <summary>Registro de pagamento, quando pago.</summary>
    public PaymentRecord? Payment { get; set; }
}
