using System.Collections.Generic;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Resultado de uma operacao de servico: o valor em caso de sucesso, ou o motivo
/// da falha. Instancias sao criadas por <see cref="ServiceResult"/>.
/// </summary>
/// <typeparam name="T">Tipo do valor devolvido em caso de sucesso.</typeparam>
internal sealed class ServiceResult<T>
{
    /// <summary>Cria o resultado.</summary>
    /// <param name="status">Desfecho da operacao.</param>
    /// <param name="value">Valor, quando houver sucesso.</param>
    /// <param name="message">Mensagem para o cliente em caso de falha.</param>
    /// <param name="errors">Erros de validacao por campo.</param>
    internal ServiceResult(
        ServiceStatus status,
        T? value,
        string message,
        IDictionary<string, string[]> errors)
    {
        Status = status;
        Value = value;
        Message = message;
        Errors = errors;
    }

    /// <summary>Desfecho da operacao.</summary>
    public ServiceStatus Status { get; }

    /// <summary>Valor produzido, presente somente em caso de sucesso.</summary>
    public T? Value { get; }

    /// <summary>Mensagem para o cliente em caso de falha.</summary>
    public string Message { get; }

    /// <summary>Erros de validacao por campo, quando <see cref="Status"/> e Invalid.</summary>
    public IDictionary<string, string[]> Errors { get; }
}
