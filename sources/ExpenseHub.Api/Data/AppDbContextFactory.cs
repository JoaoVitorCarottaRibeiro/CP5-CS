using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Fabrica usada apenas em tempo de design (por exemplo, ao gerar migrations
/// com `dotnet ef`). Permite criar o contexto sem iniciar toda a aplicacao
/// nem depender de segredos como a chave do JWT.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=expensehub.db")
            .Options;

        return new AppDbContext(options);
    }
}
