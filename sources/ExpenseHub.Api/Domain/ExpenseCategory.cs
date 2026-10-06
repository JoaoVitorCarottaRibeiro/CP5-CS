using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Categoria de uma despesa (transporte, alimentacao, etc.).
/// </summary>
internal sealed class ExpenseCategory
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public int Id { get; set; }

    /// <summary>Nome exibido da categoria.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Reembolsos associados a esta categoria.</summary>
    public ICollection<Expense> Expenses { get; } = new List<Expense>();
}
