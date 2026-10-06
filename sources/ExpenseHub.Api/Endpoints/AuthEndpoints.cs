using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Endpoints de autenticacao: login (publico) e consulta do usuario autenticado.
/// </summary>
internal static class AuthEndpoints
{
    /// <summary>Mapeia as rotas de autenticacao.</summary>
    /// <param name="routes">Construtor de rotas.</param>
    internal static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/register", RegisterAsync);
        routes.MapPost("/login", LoginAsync);
        routes.MapGet("/me", MeAsync).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager)
    {
        if (!RequestValidator.TryValidate(request, out IDictionary<string, string[]> errors))
        {
            return Results.ValidationProblem(errors);
        }

        ApplicationUser? existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "E-mail ja cadastrado.");
        }

        ApplicationUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return result.ToValidationProblem();
        }

        return Results.Created($"/api/admin/users/{user.Id}", new { user.Id, user.Email });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        TokenService tokenService)
    {
        if (!RequestValidator.TryValidate(request, out IDictionary<string, string[]> errors))
        {
            return Results.ValidationProblem(errors);
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciais invalidas.");
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        AuthResponse response = tokenService.CreateToken(user, roles);

        return Results.Ok(response);
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        ApplicationUser? user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(user);

        return Results.Ok(new { user.Id, user.Email, Roles = roles });
    }
}
