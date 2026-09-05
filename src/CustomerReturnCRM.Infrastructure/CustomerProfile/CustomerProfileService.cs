using CustomerReturnCRM.Application.CustomerManagement;
using CustomerReturnCRM.Application.ReturnAnalysis;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.CustomerProfile;

public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IReturnAnalysisService _returnAnalysis;

    public CustomerProfileService(ApplicationDbContext dbContext, IReturnAnalysisService returnAnalysis)
    {
        _dbContext = dbContext;
        _returnAnalysis = returnAnalysis;
    }

    public async Task<CustomerProfileResult?> GetAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);

        var customer = await _dbContext.Customers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == customerId, cancellationToken);
        if (customer is null) return null;

        var visits = await _dbContext.Visits.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.CustomerId == customerId)
            .OrderByDescending(x => x.VisitAt)
            .Select(x => new CustomerProfileVisit(
                x.Id, x.VisitAt, x.TotalAmount, x.Note,
                _dbContext.VisitServices.Where(vs => vs.VisitId == x.Id).OrderBy(vs => vs.ServiceTitle).Select(vs => vs.ServiceTitle).ToList()))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var appointments = await _dbContext.Appointments.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.CustomerId == customerId && x.StartAt >= now &&
                        x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.NoShow)
            .OrderBy(x => x.StartAt)
            .Select(x => new CustomerProfileAppointment(
                x.Id, x.StartAt, x.EndAt, x.Status.ToString(), x.Note,
                _dbContext.AppointmentServices.Where(a => a.AppointmentId == x.Id).OrderBy(a => a.ServiceTitle).Select(a => a.ServiceTitle).ToList()))
            .ToListAsync(cancellationToken);

        var reminders = await _dbContext.Reminders.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.CustomerId == customerId && x.Status == ReminderStatus.Pending)
            .OrderBy(x => x.DueAt)
            .Select(x => new CustomerProfileReminder(x.Id, x.ServiceId, x.Title, x.DueAt, x.Status.ToString(), x.Note))
            .ToListAsync(cancellationToken);

        var customerResult = new CustomerResult(
            customer.Id, customer.BusinessId, customer.FirstName, customer.LastName, customer.Mobile,
            customer.BirthDate, customer.Note, customer.IsActive, customer.CreatedAt, customer.UpdatedAt,
            visits.Select(x => (DateTime?)x.VisitAt).FirstOrDefault(), visits.Count);

        var analysis = await _returnAnalysis.GetCustomerAnalysisAsync(businessId, customerId, userId, cancellationToken)
            ?? new CustomerReturnAnalysisResult(customerId,
                string.IsNullOrWhiteSpace(customer.LastName) ? customer.FirstName : $"{customer.FirstName} {customer.LastName}",
                customer.Mobile, Array.Empty<ExpectedReturnResult>());

        return new CustomerProfileResult(customerResult, visits, appointments, reminders, analysis);
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken))
            throw new UnauthorizedAccessException();
    }
}
