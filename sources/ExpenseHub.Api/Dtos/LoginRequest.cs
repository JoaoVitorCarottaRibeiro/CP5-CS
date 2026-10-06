using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Credenciais de login.
/// </summary>
internal sealed class LoginRequest
{
    /// <summary>E-mail do usuario.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha do usuario.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
