using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Endpoints administrativos, restritos a role Admin: listagem de usuarios e
/// gerenciamento de roles.
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>Nome da politica de autorizacao exclusiva de Admin.</summary>
    internal const string AdminPolicy = "AdminOnly";

    /// <summary>Mapeia as rotas administrativas.</summary>
    /// <param name="routes">Construtor de rotas.</param>
    internal static void MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/admin/users", ListUsersAsync)
            .RequireAuthorization(AdminPolicy);

        routes.MapPut("/api/admin/users/{id}/roles", UpdateRolesAsync)
            .RequireAuthorization(AdminPolicy);
    }

    private static async Task<IResult> ListUsersAsync(UserManager<ApplicationUser> userManager)
    {
        List<ApplicationUser> users = await userManager.Users.ToListAsync();
        List<UserDto> result = new(users.Count);

        foreach (ApplicationUser user in users)
        {
            IList<string> roles = await userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToArray(),
            });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateRolesAsync(
        string id,
        UpdateRolesRequest request,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal caller)
    {
        if (!RequestValidator.TryValidate(request, out IDictionary<string, string[]> errors))
        {
            return Results.ValidationProblem(errors);
        }

        string[] requestedRoles = request.Roles
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        string[] unknownRoles = requestedRoles
            .Where(role => !Roles.All.Contains(role))
            .ToArray();

        if (unknownRoles.Length > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: $"Roles desconhecidas: {string.Join(", ", unknownRoles)}.");
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        string? callerId = userManager.GetUserId(caller);
        bool isSelf = string.Equals(callerId, user.Id, StringComparison.Ordinal);
        if (isSelf && !requestedRoles.Contains(Roles.Admin))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "O Admin nao pode remover a propria role Admin.");
        }

        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        string[] rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.Ordinal).ToArray();
        string[] rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.Ordinal).ToArray();

        if (rolesToRemove.Length > 0)
        {
            IdentityResult removed = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removed.Succeeded)
            {
                return removed.ToValidationProblem();
            }
        }

        if (rolesToAdd.Length > 0)
        {
            IdentityResult added = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!added.Succeeded)
            {
                return added.ToValidationProblem();
            }
        }

        IList<string> updatedRoles = await userManager.GetRolesAsync(user);

        return Results.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = updatedRoles.ToArray(),
        });
    }
}
