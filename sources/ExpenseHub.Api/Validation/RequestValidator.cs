using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ExpenseHub.Api.Validation;

/// <summary>
/// Valida DTOs com base nas DataAnnotations declaradas. Usado pelos endpoints
/// minimais, que nao executam a validacao automatica dos controllers.
/// </summary>
internal static class RequestValidator
{
    /// <summary>Valida o modelo e devolve os erros agrupados por campo.</summary>
    /// <param name="model">Objeto a validar.</param>
    /// <param name="errors">Erros por campo, vazio quando valido.</param>
    /// <returns><c>true</c> quando o modelo e valido.</returns>
    internal static bool TryValidate(object model, out IDictionary<string, string[]> errors)
    {
        ValidationContext context = new(model);
        List<ValidationResult> results = new();
        bool isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        if (isValid)
        {
            errors = new Dictionary<string, string[]>();
            return true;
        }

        errors = results
            .SelectMany(
                result => result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, member) => (Member: member, Message: result.ErrorMessage ?? string.Empty))
            .GroupBy(entry => entry.Member, entry => entry.Message)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return false;
    }
}
