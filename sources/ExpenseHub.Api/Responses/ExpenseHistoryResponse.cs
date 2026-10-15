using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Responses;

/// <summary>Representacao de uma acao registrada no historico do reembolso.</summary>
internal sealed class ExpenseHistoryResponse
{
    /// <summary>Acao executada.</summary>
    public ExpenseAction Action { get; init; }

    /// <summary>Identificador do usuario que executou a acao.</summary>
    public string ActorId { get; init; } = string.Empty;

    /// <summary>Instante da acao em UTC.</summary>
    public DateTime OccurredAtUtc { get; init; }

    /// <summary>Estado anterior.</summary>
    public ExpenseState PreviousState { get; init; }

    /// <summary>Estado posterior.</summary>
    public ExpenseState NextState { get; init; }

    /// <summary>Justificativa da reprovacao, quando aplicavel.</summary>
    public string? Justification { get; init; }

    /// <summary>Alteracoes feitas no rascunho, quando aplicavel.</summary>
    public string? Changes { get; init; }

}
