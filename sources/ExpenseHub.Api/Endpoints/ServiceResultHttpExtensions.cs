using System;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Converte resultados de servico em respostas HTTP com ProblemDetails.
/// </summary>
internal static class ServiceResultHttpExtensions
{
    /// <summary>Converte o resultado, usando a fabrica informada no caso de sucesso.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="result">Resultado do servico.</param>
    /// <param name="onSuccess">Resposta de sucesso a partir do valor.</param>
    /// <returns>Resposta HTTP.</returns>
    internal static IResult ToHttpResult<T>(this ServiceResult<T> result, Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            ServiceStatus.Success => onSuccess(result.Value!),
            ServiceStatus.Invalid => Results.ValidationProblem(result.Errors),
            ServiceStatus.Forbidden => Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: result.Message),
            ServiceStatus.NotFound => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: result.Message),
            ServiceStatus.Conflict => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: result.Message),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Converte o resultado, devolvendo 200 com o valor no caso de sucesso.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="result">Resultado do servico.</param>
    /// <returns>Resposta HTTP.</returns>
    internal static IResult ToHttpResult<T>(this ServiceResult<T> result)
    {
        return result.ToHttpResult(value => Results.Ok(value));
    }
}
