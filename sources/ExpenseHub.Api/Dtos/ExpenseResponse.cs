using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Representacao de um reembolso devolvida pela API.
/// </summary>
internal sealed class ExpenseResponse
{
    /// <summary>Identificador do reembolso.</summary>
    public int Id { get; init; }

    /// <summary>Identificador do proprietario.</summary>
    public string OwnerId { get; init; } = string.Empty;

    /// <summary>Categoria da despesa.</summary>
    public int CategoryId { get; init; }

    /// <summary>Nome da categoria.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Descricao da despesa.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Valor em reais.</summary>
    public decimal Amount { get; init; }

    /// <summary>Data da despesa.</summary>
    public DateOnly ExpenseDate { get; init; }

    /// <summary>Estado atual.</summary>
    public ExpenseState State { get; init; }

    /// <summary>Instante de criacao em UTC.</summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Instante da ultima atualizacao em UTC.</summary>
    public DateTime UpdatedAtUtc { get; init; }

    /// <summary>Converte a entidade, que deve estar com a categoria carregada.</summary>
    /// <param name="expense">Reembolso persistido.</param>
    /// <returns>Representacao para a API.</returns>
    internal static ExpenseResponse FromEntity(Expense expense)
    {
        return new ExpenseResponse
        {
            Id = expense.Id,
            OwnerId = expense.OwnerId,
            CategoryId = expense.CategoryId,
            CategoryName = expense.Category?.Name ?? string.Empty,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseDate = DateOnly.FromDateTime(expense.ExpenseDate),
            State = expense.State,
            CreatedAtUtc = expense.CreatedAtUtc,
            UpdatedAtUtc = expense.UpdatedAtUtc,
        };
    }
}
