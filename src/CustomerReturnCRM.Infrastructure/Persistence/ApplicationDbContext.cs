using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMember> BusinessMembers => Set<BusinessMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Business>(entity =>
        {
            entity.ToTable("Businesses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BusinessType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Mobile).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.Mobile);
        });

        builder.Entity<BusinessMember>(entity =>
        {
            entity.ToTable("BusinessMembers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Business).WithMany(x => x.Members).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
