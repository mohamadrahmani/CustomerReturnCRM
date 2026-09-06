using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.Seeding;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        const string email = "admin@demo.local";
        const string password = "Admin@123";
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        var businessId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        if (!await db.Businesses.AnyAsync(x => x.Id == businessId, cancellationToken))
            db.Businesses.Add(new Business { Id=businessId, Name="آرایشگاه نمونه آریا", BusinessType="BeautySalon", Mobile="09120000001", Address="خیابان ولیعصر، پلاک ۱۲۵", City="تهران", IsActive=true, CreatedAt=now });
        if (!await db.BusinessMembers.AnyAsync(x => x.BusinessId==businessId && x.UserId==user.Id, cancellationToken))
            db.BusinessMembers.Add(new BusinessMember { Id=Guid.NewGuid(), BusinessId=businessId, UserId=user.Id, Role="Owner", CreatedAt=now });

        var staff = new[] { ("30000000-0000-0000-0000-000000000001","محمد","رحمانی","09120000002",user.Id), ("30000000-0000-0000-0000-000000000002","سارا","احمدی","09120000003",(Guid?)null), ("30000000-0000-0000-0000-000000000003","نگار","کریمی","09120000004",(Guid?)null) };
        foreach (var s in staff) if (!await db.Staff.AnyAsync(x=>x.Id==Guid.Parse(s.Item1),cancellationToken)) db.Staff.Add(new Staff { Id=Guid.Parse(s.Item1),BusinessId=businessId,FirstName=s.Item2,LastName=s.Item3,Mobile=s.Item4,UserId=s.Item5,IsActive=true,CreatedAt=now });

        var services = new[] { ("40000000-0000-0000-0000-000000000001","اصلاح مو",450000m,45,45), ("40000000-0000-0000-0000-000000000002","رنگ مو",1800000m,120,60), ("40000000-0000-0000-0000-000000000003","ناخن",650000m,90,21), ("40000000-0000-0000-0000-000000000004","فیشال",900000m,60,30), ("40000000-0000-0000-0000-000000000005","کراتین مو",3200000m,180,90) };
        foreach (var s in services) if (!await db.Services.AnyAsync(x=>x.Id==Guid.Parse(s.Item1),cancellationToken)) db.Services.Add(new Service { Id=Guid.Parse(s.Item1),BusinessId=businessId,Title=s.Item2,Description="داده آزمایشی",DefaultPrice=s.Item3,DefaultDurationMinutes=s.Item4,SuggestedReturnDays=s.Item5,IsActive=true,CreatedAt=now });

        var customers = new[] { (1,"مریم","احمدی","09121111101",70),(2,"سارا","محمدی","09121111102",75),(3,"نگار","حسینی","09121111103",25),(4,"الهام","کریمی","09121111104",15),(5,"نازنین","رضایی","09121111105",50),(6,"پریسا","کاظمی","09121111106",100),(7,"مهسا","مرادی","09121111107",10),(8,"شیدا","موسوی","09121111108",130),(9,"ریحانه","صادقی","09121111109",5),(10,"بهاره","حیدری","09121111110",35),(11,"آیدا","نوری","09121111111",20),(12,"لیلا","اکبری","09121111112",90) };
        foreach (var c in customers) { var id=Guid.Parse($"50000000-0000-0000-0000-0000000000{c.Item1:00}"); if (!await db.Customers.AnyAsync(x=>x.Id==id,cancellationToken)) db.Customers.Add(new Customer { Id=id,BusinessId=businessId,FirstName=c.Item2,LastName=c.Item3,Mobile=c.Item4,BirthDate=new DateTime(1990,1,15).AddDays(c.Item5),Note="داده آزمایشی توسعه",IsActive=true,CreatedAt=now.AddDays(-c.Item5-100) }); }
        await db.SaveChangesAsync(cancellationToken);

        var staff1=Guid.Parse("30000000-0000-0000-0000-000000000001"); var staff2=Guid.Parse("30000000-0000-0000-0000-000000000002"); var staff3=Guid.Parse("30000000-0000-0000-0000-000000000003");
        var haircut=Guid.Parse("40000000-0000-0000-0000-000000000001"); var color=Guid.Parse("40000000-0000-0000-0000-000000000002"); var nail=Guid.Parse("40000000-0000-0000-0000-000000000003"); var facial=Guid.Parse("40000000-0000-0000-0000-000000000004"); var keratin=Guid.Parse("40000000-0000-0000-0000-000000000005");

        var visitData=new[] { (1,haircut,staff1,"اصلاح مو",450000m,45,45),(2,color,staff2,"رنگ مو",1800000m,120,60),(3,nail,staff3,"ناخن",650000m,90,21),(4,facial,staff2,"فیشال",900000m,60,30),(5,haircut,staff1,"اصلاح مو",450000m,45,45),(6,keratin,staff2,"کراتین مو",3200000m,180,90),(7,nail,staff3,"ناخن",650000m,90,21),(8,color,staff2,"رنگ مو",1800000m,120,60) };
        foreach(var v in visitData){var id=Guid.Parse($"60000000-0000-0000-0000-00000000000{v.Item1}"); if(await db.Visits.AnyAsync(x=>x.Id==id,cancellationToken))continue; var cid=Guid.Parse($"50000000-0000-0000-0000-0000000000{v.Item1:00}"); var at=now.AddDays(-v.Item1*10); db.Visits.Add(new Visit{Id=id,BusinessId=businessId,CustomerId=cid,VisitAt=at,TotalAmount=v.Item5,Note=v.Item4,CreatedAt=at}); db.VisitServices.Add(new VisitService{Id=Guid.NewGuid(),VisitId=id,ServiceId=v.Item2,StaffId=v.Item3,ServiceTitle=v.Item4,Price=v.Item5,DurationMinutes=v.Item6,SuggestedReturnDays=v.Item7});}
        await db.SaveChangesAsync(cancellationToken);

        var appointments=new[] { (1,haircut,staff1,45,450000m,AppointmentStatus.Confirmed),(2,color,staff2,120,1800000m,AppointmentStatus.Pending),(5,haircut,staff1,45,450000m,AppointmentStatus.Confirmed),(9,facial,staff2,60,900000m,AppointmentStatus.Pending) };
        foreach(var a in appointments){var id=Guid.Parse($"70000000-0000-0000-0000-00000000000{a.Item1}");if(await db.Appointments.AnyAsync(x=>x.Id==id,cancellationToken))continue;var cid=Guid.Parse($"50000000-0000-0000-0000-0000000000{a.Item1:00}");var start=now.AddDays(a.Item1%4+1);db.Appointments.Add(new Appointment{Id=id,BusinessId=businessId,CustomerId=cid,StartAt=start,EndAt=start.AddMinutes(a.Item4),Status=a.Item6,Note="نوبت آزمایشی",CreatedAt=now});db.AppointmentServices.Add(new AppointmentService{Id=Guid.NewGuid(),AppointmentId=id,ServiceId=a.Item2,StaffId=a.Item3,ServiceTitle=a.Item2==haircut?"اصلاح مو":a.Item2==color?"رنگ مو":"فیشال",Price=a.Item5,DurationMinutes=a.Item4});}
        await db.SaveChangesAsync(cancellationToken);

        if(!await db.Reminders.AnyAsync(x=>x.BusinessId==businessId,cancellationToken)) db.Reminders.AddRange(new Reminder{Id=Guid.Parse("80000000-0000-0000-0000-000000000001"),BusinessId=businessId,CustomerId=Guid.Parse("50000000-0000-0000-0000-000000000001"),ServiceId=haircut,Title="پیگیری بازگشت مشتری",DueAt=now.AddDays(-2),Status=ReminderStatus.Pending,Note="مشتری از زمان پیشنهادی بازگشت گذشته است",CreatedByUserId=user.Id,CreatedAt=now},new Reminder{Id=Guid.Parse("80000000-0000-0000-0000-000000000002"),BusinessId=businessId,CustomerId=Guid.Parse("50000000-0000-0000-0000-000000000002"),ServiceId=color,Title="یادآوری رنگ مو",DueAt=now.AddDays(5),Status=ReminderStatus.Pending,CreatedByUserId=user.Id,CreatedAt=now});
        var template=Guid.Parse("90000000-0000-0000-0000-000000000001"); if(!await db.SmsTemplates.AnyAsync(x=>x.Id==template,cancellationToken))db.SmsTemplates.Add(new SmsTemplate{Id=template,BusinessId=businessId,Name="یادآوری بازگشت",Content="سلام {{FirstName}} عزیز، زمان مراجعه مجدد شما به آریا نزدیک شده است.",IsActive=true,CreatedAt=now});
        var campaign=Guid.Parse("91000000-0000-0000-0000-000000000001"); if(!await db.SmsCampaigns.AnyAsync(x=>x.Id==campaign,cancellationToken)){db.SmsCampaigns.Add(new SmsCampaign{Id=campaign,BusinessId=businessId,TemplateId=template,CreatedByUserId=user.Id,Name="کمپین بازگشت شهریور",Message="سلام {{FirstName}} عزیز، زمان مراجعه مجدد شما نزدیک شده است.",ScheduledAt=now.AddDays(-2),Status=SmsCampaignStatus.Completed,StartedAt=now.AddDays(-2),CompletedAt=now.AddDays(-2),CreatedAt=now.AddDays(-2)});await db.SaveChangesAsync(cancellationToken);var cs=await db.Customers.Where(x=>x.BusinessId==businessId).Take(4).ToListAsync(cancellationToken);foreach(var c in cs)db.SmsRecipients.Add(new SmsRecipient{Id=Guid.NewGuid(),SmsCampaignId=campaign,CustomerId=c.Id,Mobile=c.Mobile,RenderedMessage=$"سلام {c.FirstName} عزیز، زمان مراجعه مجدد شما نزدیک شده است.",Status=SmsRecipientStatus.Delivered,ProviderMessageId=$"DEMO-{c.Id:N}".Substring(0,12),SubmittedAt=now.AddDays(-2),DeliveredAt=now.AddDays(-2),CreatedAt=now.AddDays(-2)});}
        await db.SaveChangesAsync(cancellationToken);
    }
}
