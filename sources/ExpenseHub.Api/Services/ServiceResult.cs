using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Fabricas de <see cref="ServiceResult{T}"/>.
/// </summary>
internal static class ServiceResult
{
    /// <summary>Resultado de sucesso.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="value">Valor produzido.</param>
    /// <returns>Resultado com status Success.</returns>
    internal static ServiceResult<T> Success<T>(T value)
    {
        return new ServiceResult<T>(ServiceStatus.Success, value, string.Empty, new Dictionary<string, string[]>());
    }

    /// <summary>Resultado de falha sem erros por campo.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="status">Motivo da falha.</param>
    /// <param name="message">Mensagem para o cliente.</param>
    /// <returns>Resultado de falha.</returns>
    internal static ServiceResult<T> Failure<T>(ServiceStatus status, string message)
    {
        return new ServiceResult<T>(status, default, message, new Dictionary<string, string[]>());
    }

    /// <summary>Resultado de entrada invalida em um campo.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="field">Campo invalido.</param>
    /// <param name="message">Descricao do erro.</param>
    /// <returns>Resultado com status Invalid.</returns>
    internal static ServiceResult<T> Invalid<T>(string field, string message)
    {
        Dictionary<string, string[]> errors = new()
        {
            [field] = new[] { message },
        };

        return new ServiceResult<T>(ServiceStatus.Invalid, default, message, errors);
    }

    /// <summary>Converte uma decisao negativa da politica em falha.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="decision">Decisao diferente de Allowed.</param>
    /// <param name="conflictMessage">Mensagem usada quando a decisao e Conflict.</param>
    /// <returns>Resultado de falha correspondente.</returns>
    internal static ServiceResult<T> Denied<T>(AccessDecision decision, string conflictMessage)
    {
        return decision switch
        {
            AccessDecision.NotFound => Failure<T>(ServiceStatus.NotFound, "Reembolso nao encontrado."),
            AccessDecision.Forbidden => Failure<T>(ServiceStatus.Forbidden, "Operacao nao permitida para este usuario."),
            AccessDecision.Conflict => Failure<T>(ServiceStatus.Conflict, conflictMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, "Decisao nao representa uma falha."),
        };
    }
}
