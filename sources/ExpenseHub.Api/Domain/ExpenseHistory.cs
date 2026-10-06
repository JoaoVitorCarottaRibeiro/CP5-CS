using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro imutavel de uma acao ocorrida sobre um reembolso. Guarda o estado
/// anterior e posterior, o ator, o instante em UTC e dados complementares.
/// </summary>
internal sealed class ExpenseHistory
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public int Id { get; set; }

    /// <summary>Reembolso relacionado.</summary>
    public int ExpenseId { get; set; }

    /// <summary>Reembolso relacionado.</summary>
    public Expense? Expense { get; set; }

    /// <summary>Acao registrada.</summary>
    public ExpenseAction Action { get; set; }

    /// <summary>Identificador do usuario que executou a acao.</summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>Usuario que executou a acao.</summary>
    public ApplicationUser? Actor { get; set; }

    /// <summary>Instante da acao em UTC.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Estado anterior a acao.</summary>
    public ExpenseState PreviousState { get; set; }

    /// <summary>Estado posterior a acao.</summary>
    public ExpenseState NextState { get; set; }

    /// <summary>Justificativa, obrigatoria na reprovacao.</summary>
    public string? Justification { get; set; }

    /// <summary>Resumo das alteracoes feitas enquanto em Draft.</summary>
    public string? Changes { get; set; }
}
