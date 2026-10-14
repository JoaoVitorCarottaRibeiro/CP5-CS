using System;
using System.ComponentModel.DataAnnotations;
using ExpenseHub.Api.Validation;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados aceitos na criacao e na edicao de um rascunho. Nao possui proprietario,
/// estado, atores nem horarios: esses valores sao definidos pelo servidor.
/// </summary>
internal sealed class ExpenseRequest
{
    /// <summary>Categoria da despesa; deve existir no cadastro.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    /// <summary>Descricao da despesa, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Valor em reais, entre R$ 0,01 e Int32.MaxValue.</summary>
    [Required]
    [Range(
        typeof(decimal),
        "0.01",
        "2147483647",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; set; }

    /// <summary>Data da despesa, valida e nao futura.</summary>
    [Required]
    [NotFutureDate]
    public DateOnly? ExpenseDate { get; set; }
}
