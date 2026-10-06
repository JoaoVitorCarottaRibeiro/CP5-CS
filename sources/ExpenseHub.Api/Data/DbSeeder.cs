using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Popula dados iniciais de forma idempotente: as roles obrigatorias, a unica
/// conta Admin e um conjunto base de categorias. A senha do Admin vem da
/// configuracao (user-secrets ou variavel de ambiente), nunca do codigo-fonte.
/// </summary>
internal static class DbSeeder
{
    private static readonly string[] _defaultCategories =
    {
        "Transporte",
        "Alimentacao",
        "Hospedagem",
        "Material",
    };

    /// <summary>Executa o seed usando os servicos do escopo informado.</summary>
    /// <param name="services">Provedor de servicos de um escopo.</param>
    internal static async Task SeedAsync(IServiceProvider services)
    {
        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        AppDbContext database = services.GetRequiredService<AppDbContext>();

        await EnsureRolesAsync(roleManager);
        await EnsureAdminAsync(userManager, configuration);
        await EnsureCategoriesAsync(database);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (string role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task EnsureAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        string adminEmail = configuration["Seed:AdminEmail"] ?? "admin@expensehub.local";
        string? adminPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Seed:AdminPassword nao configurado. Defina via user-secrets ou variavel de ambiente.");
        }

        ApplicationUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };

            IdentityResult created = await userManager.CreateAsync(admin, adminPassword);
            if (!created.Succeeded)
            {
                string message = string.Join("; ", created.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Falha ao criar o usuario Admin: {message}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }

    private static async Task EnsureCategoriesAsync(AppDbContext database)
    {
        if (await database.Categories.AnyAsync())
        {
            return;
        }

        IEnumerable<ExpenseCategory> categories = _defaultCategories
            .Select(name => new ExpenseCategory { Name = name });

        database.Categories.AddRange(categories);
        await database.SaveChangesAsync();
    }
}
