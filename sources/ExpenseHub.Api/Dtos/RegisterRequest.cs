using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados de cadastro de um novo usuario. O cadastro nunca aceita role; a role
/// e atribuida posteriormente por um Admin.
/// </summary>
internal sealed class RegisterRequest
{
    /// <summary>E-mail, usado tambem como nome de usuario.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha do novo usuario.</summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
