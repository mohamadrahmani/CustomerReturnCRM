using CustomerReturnCRM.Application.CustomerManagement;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.CustomerManagement;

public sealed class CustomerManagementService : ICustomerManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public CustomerManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<CustomerResult>> ListAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);

        var query = _dbContext.Customers.Where(x => x.BusinessId == businessId && x.IsActive);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerResult(
                x.Id,
                x.BusinessId,
                x.FirstName,
                x.LastName,
                x.Mobile,
                x.BirthDate,
                x.Note,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt,
                _dbContext.Visits
                    .Where(v => v.BusinessId == businessId && v.CustomerId == x.Id)
                    .OrderByDescending(v => v.VisitAt)
                    .Select(v => (DateTime?)v.VisitAt)
                    .FirstOrDefault(),
                _dbContext.Visits.Count(v => v.BusinessId == businessId && v.CustomerId == x.Id)))
            .ToListAsync(cancellationToken);

        return Pagination.Create(items, page, pageSize, total);
    }

    public async Task<CustomerResult?> GetAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);

        return await _dbContext.Customers
            .Where(x => x.BusinessId == businessId && x.Id == customerId && x.IsActive)
            .Select(x => new CustomerResult(
                x.Id,
                x.BusinessId,
                x.FirstName,
                x.LastName,
                x.Mobile,
                x.BirthDate,
                x.Note,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt,
                _dbContext.Visits
                    .Where(v => v.BusinessId == businessId && v.CustomerId == x.Id)
                    .OrderByDescending(v => v.VisitAt)
                    .Select(v => (DateTime?)v.VisitAt)
                    .FirstOrDefault(),
                _dbContext.Visits.Count(v => v.BusinessId == businessId && v.CustomerId == x.Id)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerResult> CreateAsync(Guid businessId, Guid userId, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.FirstName, request.Mobile);
        var mobile = request.Mobile.Trim();
        if (await _dbContext.Customers.AnyAsync(x => x.BusinessId == businessId && x.Mobile == mobile, cancellationToken))
            throw new InvalidOperationException("A customer with this mobile already exists in the business.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            FirstName = request.FirstName.Trim(),
            LastName = Normalize(request.LastName),
            Mobile = mobile,
            BirthDate = request.BirthDate,
            Note = Normalize(request.Note),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(customer, null, 0);
    }

    public async Task<CustomerResult?> UpdateAsync(Guid businessId, Guid customerId, Guid userId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.FirstName, request.Mobile);
        var customer = await _dbContext.Customers.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == customerId, cancellationToken);
        if (customer is null) return null;

        var mobile = request.Mobile.Trim();
        if (await _dbContext.Customers.AnyAsync(x => x.BusinessId == businessId && x.Mobile == mobile && x.Id != customerId, cancellationToken))
            throw new InvalidOperationException("A customer with this mobile already exists in the business.");

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = Normalize(request.LastName);
        customer.Mobile = mobile;
        customer.BirthDate = request.BirthDate;
        customer.Note = Normalize(request.Note);
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetAsync(businessId, customerId, userId, cancellationToken);
    }

    public async Task<bool> DeactivateAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var customer = await _dbContext.Customers.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == customerId, cancellationToken);
        if (customer is null) return false;
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken))
            throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static void Validate(string firstName, string mobile)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(mobile)) throw new ArgumentException("Mobile is required.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CustomerResult ToResult(Customer x, DateTime? lastVisitDate, int totalVisits) =>
        new(x.Id, x.BusinessId, x.FirstName, x.LastName, x.Mobile, x.BirthDate, x.Note, x.IsActive, x.CreatedAt, x.UpdatedAt, lastVisitDate, totalVisits);
}
