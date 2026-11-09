using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Models;

namespace SchoolAdmission.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Database tables
    public DbSet<AdmissionClass> AdmissionClasses => Set<AdmissionClass>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // A student registration number must be unique.
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.RegistrationNumber)
            .IsUnique();

        // One student can have only one payment record per hold.
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.AdmissionHoldId)
            .IsUnique();

        // Transaction references must be unique.
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.TransactionReference)
            .IsUnique();

        // Configure the one-to-many relationship:
        // One admission class can have many holds.
        modelBuilder.Entity<AdmissionHold>()
            .HasOne(h => h.AdmissionClass)
            .WithMany(c => c.Holds)
            .HasForeignKey(h => h.AdmissionClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // One student can have many holds.
        modelBuilder.Entity<AdmissionHold>()
            .HasOne(h => h.Student)
            .WithMany(s => s.Holds)
            .HasForeignKey(h => h.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One hold can have at most one payment.
        modelBuilder.Entity<AdmissionHold>()
            .HasOne(h => h.Payment)
            .WithOne(p => p.AdmissionHold)
            .HasForeignKey<Payment>(p => p.AdmissionHoldId)
            .OnDelete(DeleteBehavior.Restrict);

        // Store payment amounts with two decimal places.
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);
    }
}