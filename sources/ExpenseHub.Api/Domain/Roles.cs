using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Nomes das roles do sistema. Centralizar as constantes evita divergencia
/// entre o seed, a autorizacao das rotas e a validacao de atribuicao de roles.
/// </summary>
internal static class Roles
{
    /// <summary>Administra usuarios e roles.</summary>
    internal const string Admin = "Admin";

    /// <summary>Cria, edita, envia e consulta os proprios reembolsos.</summary>
    internal const string Employee = "Employee";

    /// <summary>Aprova ou reprova reembolsos nao proprios.</summary>
    internal const string Approver = "Approver";

    /// <summary>Registra o pagamento de reembolsos aprovados nao proprios.</summary>
    internal const string Finance = "Finance";

    /// <summary>Consulta todos os reembolsos e historicos, sem escrita.</summary>
    internal const string Auditor = "Auditor";

    /// <summary>Todas as roles conhecidas pelo sistema.</summary>
    internal static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        Employee,
        Approver,
        Finance,
        Auditor,
    };
}
