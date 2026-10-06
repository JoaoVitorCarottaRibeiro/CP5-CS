using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Conjunto de roles a ser aplicado a um usuario. Substitui as roles atuais
/// pelo conjunto informado; somente roles conhecidas sao aceitas.
/// </summary>
internal sealed class UpdateRolesRequest
{
    /// <summary>Roles desejadas para o usuario.</summary>
    [Required]
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
