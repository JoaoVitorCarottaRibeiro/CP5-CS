using System;
using System.Text;
using System.Threading.Tasks;
using ExpenseHub.Api.Configuration;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Endpoints;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString = builder.Configuration.GetConnectionString("Default")
            ?? "Data Source=expensehub.db";

        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddSingleton<TokenService>();

        JwtOptions jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key nao configurado. Defina via user-secrets ou variavel de ambiente.");
        }

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AdminEndpoints.AdminPolicy, policy => policy.RequireRole(Roles.Admin));

        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        WebApplication app = builder.Build();

        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
        {
            AppDbContext database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await database.Database.MigrateAsync();
            await DbSeeder.SeedAsync(scope.ServiceProvider);
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        app.MapAuthEndpoints();
        app.MapAdminEndpoints();

        await app.RunAsync();
    }
}
