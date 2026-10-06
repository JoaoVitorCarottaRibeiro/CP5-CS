using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Validation;

/// <summary>
/// Converte um <see cref="IdentityResult"/> com falhas em um resultado HTTP de
/// validacao, preservando as mensagens do Identity (senha fraca, e-mail em uso).
/// </summary>
internal static class IdentityResultExtensions
{
    /// <summary>Monta um ValidationProblem a partir dos erros do Identity.</summary>
    /// <param name="result">Resultado com falhas.</param>
    /// <returns>Resposta HTTP 400 com os erros agrupados.</returns>
    internal static IResult ToValidationProblem(this IdentityResult result)
    {
        Dictionary<string, string[]> errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

        return Results.ValidationProblem(errors);
    }
}
