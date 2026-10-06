using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto de persistencia da aplicacao. Herda do contexto do Identity para
/// armazenar usuarios e roles junto das entidades de dominio.
/// </summary>
internal sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>Inicializa o contexto com as opcoes configuradas.</summary>
    /// <param name="options">Opcoes do contexto.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>Reembolsos.</summary>
    public DbSet<Expense> Expenses => Set<Expense>();

    /// <summary>Categorias de despesa.</summary>
    public DbSet<ExpenseCategory> Categories => Set<ExpenseCategory>();

    /// <summary>Historico de acoes dos reembolsos.</summary>
    public DbSet<ExpenseHistory> ExpenseHistory => Set<ExpenseHistory>();

    /// <summary>Registros de pagamento.</summary>
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ExpenseCategory>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.OwnerId).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseHistory>(entity =>
        {
            entity.Property(h => h.Action).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.PreviousState).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.NextState).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.Justification).HasMaxLength(500);
            entity.Property(h => h.Changes).HasMaxLength(1000);

            entity.HasOne(h => h.Expense)
                .WithMany(e => e.History)
                .HasForeignKey(h => h.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.Actor)
                .WithMany()
                .HasForeignKey(h => h.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRecord>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.HasIndex(p => p.ExpenseId).IsUnique();

            entity.HasOne(p => p.Expense)
                .WithOne(e => e.Payment)
                .HasForeignKey<PaymentRecord>(p => p.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.PaidBy)
                .WithMany()
                .HasForeignKey(p => p.PaidById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
