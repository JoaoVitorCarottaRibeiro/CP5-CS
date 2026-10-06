using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Usuario da aplicacao. Estende o <see cref="IdentityUser"/> padrao e serve como
/// ponto de extensao para campos adicionais nas proximas issues.
/// </summary>
internal sealed class ApplicationUser : IdentityUser
{
}
