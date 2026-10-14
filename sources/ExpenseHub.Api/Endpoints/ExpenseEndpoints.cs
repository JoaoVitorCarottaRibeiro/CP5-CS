using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Endpoints de reembolsos. A policy da rota barra quem nao possui a role
/// necessaria (401/403); ownership e estado sao verificados no
/// <see cref="ExpenseService"/>.
/// </summary>
internal static class ExpenseEndpoints
{
    /// <summary>Policy das operacoes de escrita do proprietario (role Employee).</summary>
    internal const string EmployeePolicy = "EmployeeOnly";

    /// <summary>Policy de leitura: Employee, Approver, Finance ou Auditor.</summary>
    internal const string ReaderPolicy = "ExpenseReader";

    /// <summary>Mapeia as rotas de reembolso.</summary>
    /// <param name="routes">Construtor de rotas.</param>
    internal static void MapExpenseEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder expenses = routes.MapGroup("/api/expenses");

        expenses.MapPost(string.Empty, CreateAsync).RequireAuthorization(EmployeePolicy);
        expenses.MapPut("/{id:int}", UpdateAsync).RequireAuthorization(EmployeePolicy);
        expenses.MapGet(string.Empty, ListAsync).RequireAuthorization(ReaderPolicy);
        expenses.MapGet("/{id:int}", GetAsync).RequireAuthorization(ReaderPolicy);
        expenses.MapPost("/{id:int}/submit", SubmitAsync).RequireAuthorization(EmployeePolicy);
    }

    /// <summary>
    /// Monta o chamador a partir do token. Roles e identificador nunca sao lidos
    /// do corpo da requisicao.
    /// </summary>
    /// <param name="principal">Usuario autenticado.</param>
    /// <param name="userManager">Gerenciador de usuarios, usado para ler o identificador.</param>
    /// <returns>Chamador, ou <c>null</c> quando o token nao possui identificador.</returns>
    internal static ExpenseCaller? ToCaller(ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        string? userId = userManager.GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return new ExpenseCaller(userId, Roles.All.Where(principal.IsInRole));
    }

    private static async Task<IResult> CreateAsync(
        ExpenseRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ExpenseService service)
    {
        ExpenseCaller? caller = ToCaller(principal, userManager);
        if (caller is null)
        {
            return Results.Unauthorized();
        }

        if (!RequestValidator.TryValidate(request, out IDictionary<string, string[]> errors))
        {
            return Results.ValidationProblem(errors);
        }

        ServiceResult<ExpenseResponse> result = await service.CreateAsync(caller, request);

        return result.ToHttpResult(expense => Results.Created($"/api/expenses/{expense.Id}", expense));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        ExpenseRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ExpenseService service)
    {
        ExpenseCaller? caller = ToCaller(principal, userManager);
        if (caller is null)
        {
            return Results.Unauthorized();
        }

        if (!RequestValidator.TryValidate(request, out IDictionary<string, string[]> errors))
        {
            return Results.ValidationProblem(errors);
        }

        ServiceResult<ExpenseResponse> result = await service.UpdateAsync(caller, id, request);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ExpenseService service)
    {
        ExpenseCaller? caller = ToCaller(principal, userManager);
        if (caller is null)
        {
            return Results.Unauthorized();
        }

        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await service.ListAsync(caller);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAsync(
        int id,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ExpenseService service)
    {
        ExpenseCaller? caller = ToCaller(principal, userManager);
        if (caller is null)
        {
            return Results.Unauthorized();
        }

        ServiceResult<ExpenseResponse> result = await service.GetAsync(caller, id);

        return result.ToHttpResult();
    }

    private static async Task<IResult> SubmitAsync(
        int id,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ExpenseService service)
    {
        ExpenseCaller? caller = ToCaller(principal, userManager);
        if (caller is null)
        {
            return Results.Unauthorized();
        }

        ServiceResult<ExpenseResponse> result = await service.SubmitAsync(caller, id);

        return result.ToHttpResult();
    }
}
