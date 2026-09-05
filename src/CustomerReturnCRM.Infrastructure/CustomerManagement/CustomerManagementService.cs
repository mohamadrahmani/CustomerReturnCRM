using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Application.CustomerManagement;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.CustomerManagement;

public sealed class CustomerManagementService : ICustomerManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public CustomerManagementService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<CustomerResult>> ListAsync(
        Guid businessId,
        Guid userId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = true,
        CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);

        var query = _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId);

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                x.FirstName.Contains(normalizedSearch) ||
                (x.LastName != null && x.LastName.Contains(normalizedSearch)) ||
                x.Mobile.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Customer = x,
                LastVisitDate = _dbContext.Visits
                    .Where(v => v.BusinessId == businessId && v.CustomerId == x.Id)
                    .Max(v => (DateTime?)v.VisitAt),
                TotalVisits = _dbContext.Visits
                    .Count(v => v.BusinessId == businessId && v.CustomerId == x.Id)
            })
            .ToListAsync(cancellationToken);

        var results = items
            .Select(x => ToResult(x.Customer, x.LastVisitDate, x.TotalVisits))
            .ToList();

        return Pagination.Create(results, page, pageSize, totalCount);
    }

    public async Task<CustomerResult?> GetAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == customerId, cancellationToken);

        if (customer is null) return null;

        var visitStats = await _dbContext.Visits
            .Where(x => x.BusinessId == businessId && x.CustomerId == customerId)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                LastVisitDate = g.Max(x => (DateTime?)x.VisitAt),
                TotalVisits = g.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return ToResult(customer, visitStats?.LastVisitDate, visitStats?.TotalVisits ?? 0);
    }

    public async Task<CustomerResult> CreateAsync(Guid businessId, Guid userId, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.FirstName, request.Mobile);

        var mobile = request.Mobile.Trim();
        var exists = await _dbContext.Customers.AnyAsync(
            x => x.BusinessId == businessId && x.Mobile == mobile,
            cancellationToken);
        if (exists) throw new InvalidOperationException("A customer with this mobile number already exists.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            FirstName = request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            Mobile = mobile,
            BirthDate = request.BirthDate,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
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

        var customer = await _dbContext.Customers
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == customerId, cancellationToken);
        if (customer is null) return null;

        var mobile = request.Mobile.Trim();
        var duplicate = await _dbContext.Customers.AnyAsync(
            x => x.BusinessId == businessId && x.Mobile == mobile && x.Id != customerId,
            cancellationToken);
        if (duplicate) throw new InvalidOperationException("A customer with this mobile number already exists.");

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        customer.Mobile = mobile;
        customer.BirthDate = request.BirthDate;
        customer.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var visitStats = await _dbContext.Visits
            .Where(x => x.BusinessId == businessId && x.CustomerId == customerId)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                LastVisitDate = g.Max(x => (DateTime?)x.VisitAt),
                TotalVisits = g.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return ToResult(customer, visitStats?.LastVisitDate, visitStats?.TotalVisits ?? 0);
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
        var isMember = await _dbContext.BusinessMembers.AnyAsync(
            x => x.BusinessId == businessId && x.UserId == userId,
            cancellationToken);
        if (!isMember) throw new UnauthorizedAccessException();
    }

    private static CustomerResult ToResult(Customer customer, DateTime? lastVisit, int totalVisits) => new(
        customer.Id, customer.BusinessId, customer.FirstName, customer.LastName, customer.Mobile,
        customer.BirthDate, customer.Note, customer.IsActive, customer.CreatedAt, customer.UpdatedAt,
        lastVisit, totalVisits);

    private static void Validate(string firstName, string mobile)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(mobile)) throw new ArgumentException("Mobile is required.");
    }
}
