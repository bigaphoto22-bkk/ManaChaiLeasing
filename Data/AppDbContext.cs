using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PawnTicket> PawnTickets => Set<PawnTicket>();
    public DbSet<PawnTransaction> PawnTransactions => Set<PawnTransaction>();
    public DbSet<SmartLookupValue> SmartLookupValues => Set<SmartLookupValue>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(DatabasePaths.ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.Property(x => x.StoreName)
                .IsRequired();

            entity.Property(x => x.InterestRatePercent)
                .HasPrecision(8, 2);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.CitizenId)
                .IsUnique();

            entity.Property(x => x.FirstName)
                .IsRequired();

            entity.Property(x => x.LastName)
                .IsRequired();
        });

        modelBuilder.Entity<PawnTicket>(entity =>
        {
            entity.HasIndex(x => x.TicketNumber)
                .IsUnique();

            entity.Property(x => x.TicketNumber)
                .IsRequired();

            entity.Property(x => x.PrincipalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.InterestRatePercent)
                .HasPrecision(8, 2);

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.PawnTickets)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PawnTransaction>(entity =>
        {
            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.TransactionType)
                .HasConversion<string>();

            entity.Property(x => x.CashFlowType)
                .HasConversion<string>();

            entity.HasOne(x => x.PawnTicket)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.PawnTicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SmartLookupValue>(entity =>
        {
            entity.HasIndex(x => new
                {
                    x.FieldType,
                    x.Category,
                    x.NormalizedValue
                })
                .IsUnique();

            entity.Property(x => x.FieldType)
                .IsRequired();

            entity.Property(x => x.Category)
                .IsRequired();

            entity.Property(x => x.Value)
                .IsRequired();

            entity.Property(x => x.NormalizedValue)
                .IsRequired();
        });
    }
}
