using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Validation;

/// <summary>
/// Rejeita datas posteriores ao dia atual em UTC. Valores nulos sao aceitos
/// aqui; a obrigatoriedade fica a cargo de <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class NotFutureDateAttribute : ValidationAttribute
{
    /// <summary>Cria o atributo com a mensagem padrao.</summary>
    public NotFutureDateAttribute()
        : base("A data da despesa nao pode ser futura.")
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        if (value is not DateOnly date)
        {
            return true;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        return date <= today;
    }
}
