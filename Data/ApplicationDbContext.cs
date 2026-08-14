using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Unit> Units { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<CardBrand> CardBrands { get; set; }
    public DbSet<FeeRule> FeeRules { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentLog> PaymentLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de Unit
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Active).IsRequired();
        });

        // Configuração de PaymentMethod
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RequiresBrand).IsRequired();
            entity.Property(e => e.RequiresInstallments).IsRequired();
            entity.Property(e => e.Active).IsRequired();
        });

        // Configuração de CardBrand
        modelBuilder.Entity<CardBrand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Active).IsRequired();
        });

        // Configuração de FeeRule
        modelBuilder.Entity<FeeRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.PaymentMethod)
                .WithMany()
                .HasForeignKey(e => e.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.CardBrand)
                .WithMany()
                .HasForeignKey(e => e.CardBrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Installments).IsRequired(false);
            entity.Property(e => e.FeePercentage).IsRequired().HasPrecision(5, 2);
            entity.Property(e => e.Active).IsRequired();

            // Índice para busca eficiente de regras
            entity.HasIndex(e => new { e.PaymentMethodId, e.CardBrandId, e.Installments, e.Active });
        });

        // Configuração de Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PaymentMethod)
                .WithMany()
                .HasForeignKey(e => e.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CardBrand)
                .WithMany()
                .HasForeignKey(e => e.CardBrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.PatientCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PaymentDate).IsRequired();
            entity.Property(e => e.GrossAmount).IsRequired().HasPrecision(10, 2);
            entity.Property(e => e.Installments).IsRequired(false);
            entity.Property(e => e.FeePercentageApplied).IsRequired().HasPrecision(5, 2);
            entity.Property(e => e.FeeAmount).IsRequired().HasPrecision(10, 2);
            entity.Property(e => e.NetAmountExpected).IsRequired().HasPrecision(10, 2);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ConfirmedAt).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configuração de PaymentLog
        modelBuilder.Entity<PaymentLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChangedBy).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OldValue).IsRequired(false);
            entity.Property(e => e.NewValue).IsRequired(false);
            entity.Property(e => e.ChangedAt).IsRequired();
        });
    }
}
