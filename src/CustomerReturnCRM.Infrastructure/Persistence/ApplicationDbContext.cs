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
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceTemplate> ServiceTemplates => Set<ServiceTemplate>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentService> AppointmentServices => Set<AppointmentService>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitService> VisitServices => Set<VisitService>();
    public DbSet<SmartListDismissal> SmartListDismissals => Set<SmartListDismissal>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<SmsCampaign> SmsCampaigns => Set<SmsCampaign>();
    public DbSet<SmsRecipient> SmsRecipients => Set<SmsRecipient>();

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

        builder.Entity<Staff>(entity =>
        {
            entity.ToTable("Staff");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Mobile).HasMaxLength(30);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.BusinessId);
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.Business).WithMany(x => x.Staff).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
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

        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Mobile).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.Mobile }).IsUnique();
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Service>(entity =>
        {
            entity.ToTable("Services");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DefaultPrice).HasPrecision(18, 2);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.Title }).IsUnique();
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceTemplate>(entity =>
        {
            entity.ToTable("ServiceTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BusinessType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessType, x.Title }).IsUnique();
            entity.HasData(
                new ServiceTemplate { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), BusinessType = "General", Title = "General consultation", DefaultDurationMinutes = 60, SuggestedReturnDays = 30, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceTemplate { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), BusinessType = "BeautySalon", Title = "Hair coloring", DefaultDurationMinutes = 120, SuggestedReturnDays = 60, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceTemplate { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), BusinessType = "BeautySalon", Title = "Nail service", DefaultDurationMinutes = 90, SuggestedReturnDays = 21, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceTemplate { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), BusinessType = "BeautySalon", Title = "Facial", DefaultDurationMinutes = 60, SuggestedReturnDays = 30, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceTemplate { Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"), BusinessType = "BeautySalon", Title = "Haircut", DefaultDurationMinutes = 45, SuggestedReturnDays = 45, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) });
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.StartAt });
            entity.HasIndex(x => new { x.BusinessId, x.CustomerId, x.StartAt });
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AppointmentService>(entity =>
        {
            entity.ToTable("AppointmentServices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ServiceTitle).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.DurationMinutes).IsRequired();
            entity.HasIndex(x => x.AppointmentId);
            entity.HasIndex(x => x.ServiceId);
            entity.HasIndex(x => x.StaffId);
            entity.HasOne(x => x.Appointment).WithMany(x => x.AppointmentServices).HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Visit>(entity =>
        {
            entity.ToTable("Visits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.VisitAt });
            entity.HasIndex(x => new { x.BusinessId, x.CustomerId, x.VisitAt });
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VisitService>(entity =>
        {
            entity.ToTable("VisitServices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ServiceTitle).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.DurationMinutes).IsRequired();
            entity.HasIndex(x => x.VisitId);
            entity.HasIndex(x => x.ServiceId);
            entity.HasIndex(x => x.StaffId);
            entity.HasOne(x => x.Visit).WithMany(x => x.VisitServices).HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SmartListDismissal>(entity =>
        {
            entity.ToTable("SmartListDismissals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SmartListType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.CustomerId, x.ServiceId, x.SmartListType }).IsUnique();
            entity.HasIndex(x => new { x.BusinessId, x.SmartListType });
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomerReturnCRM.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.DismissedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Reminder>(entity =>
        {
            entity.ToTable("Reminders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.Status, x.DueAt });
            entity.HasIndex(x => new { x.BusinessId, x.CustomerId, x.DueAt });
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomerReturnCRM.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SmsTemplate>(entity =>
        {
            entity.ToTable("SmsTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.BusinessId, x.IsActive });
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SmsCampaign>(entity =>
        {
            entity.ToTable("SmsCampaigns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.BusinessId, x.Status, x.ScheduledAt });
            entity.HasIndex(x => new { x.BusinessId, x.CreatedByUserId });
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SmsRecipient>(entity =>
        {
            entity.ToTable("SmsRecipients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Mobile).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.ProviderMessageId).HasMaxLength(200);
            entity.Property(x => x.FailureReason).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.SmsCampaignId, x.CustomerId }).IsUnique();
            entity.HasIndex(x => new { x.SmsCampaignId, x.Status });
            entity.HasIndex(x => x.ProviderMessageId);
            entity.HasOne(x => x.SmsCampaign).WithMany(x => x.Recipients).HasForeignKey(x => x.SmsCampaignId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
