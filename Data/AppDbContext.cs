using BloodConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Donor> Donors { get; set; }
    public DbSet<BloodRequest> BloodRequests { get; set; }
    public DbSet<Donation> Donations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Donor>()
            .HasIndex(d => d.Email)
            .IsUnique();

        // Deleting a donor removes their donation records
        modelBuilder.Entity<Donation>()
            .HasOne(d => d.Donor)
            .WithMany(x => x.Donations)
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a request keeps the donation but clears the link
        modelBuilder.Entity<Donation>()
            .HasOne(d => d.BloodRequest)
            .WithMany(r => r.Donations)
            .HasForeignKey(d => d.BloodRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
