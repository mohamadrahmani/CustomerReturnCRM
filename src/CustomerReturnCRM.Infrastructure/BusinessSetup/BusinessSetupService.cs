using CustomerReturnCRM.Application.BusinessSetup;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Identity;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.BusinessSetup;

public sealed class BusinessSetupService : IBusinessSetupService
{
    private const string OwnerRole = "Owner";
    private readonly ApplicationDbContext _dbContext;

    public BusinessSetupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessSetupResult> CreateAsync(
        BusinessSetupRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        if (!await _dbContext.Set<ApplicationUser>().AnyAsync(user => user.Id == userId, cancellationToken))
        {
            throw new InvalidOperationException("The authenticated user does not exist.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            BusinessType = request.BusinessType.Trim(),
            Mobile = request.Mobile.Trim(),
            Address = NormalizeOptional(request.Address),
            City = NormalizeOptional(request.City),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var membership = new BusinessMember
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            UserId = userId,
            Role = OwnerRole,
            CreatedAt = DateTime.UtcNow
        };

        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Mobile = NormalizeOptional(request.StaffMobile),
            UserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Businesses.Add(business);
        _dbContext.BusinessMembers.Add(membership);
        _dbContext.Staff.Add(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new BusinessSetupResult(business.Id, membership.Id, staff.Id);
    }

    private static void Validate(BusinessSetupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Business name is required.");
        if (string.IsNullOrWhiteSpace(request.BusinessType)) throw new ArgumentException("Business type is required.");
        if (string.IsNullOrWhiteSpace(request.Mobile)) throw new ArgumentException("Business mobile is required.");
        if (string.IsNullOrWhiteSpace(request.FirstName)) throw new ArgumentException("Owner first name is required.");
        if (string.IsNullOrWhiteSpace(request.LastName)) throw new ArgumentException("Owner last name is required.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
