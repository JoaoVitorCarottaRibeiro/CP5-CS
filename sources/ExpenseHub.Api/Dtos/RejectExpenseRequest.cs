using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>Dados aceitos para reprovar um reembolso enviado.</summary>
internal sealed class RejectExpenseRequest
{
    /// <summary>Motivo da reprovacao, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Justification { get; set; } = string.Empty;
}
