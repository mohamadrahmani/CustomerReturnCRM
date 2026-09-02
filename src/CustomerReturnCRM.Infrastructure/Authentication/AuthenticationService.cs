using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CustomerReturnCRM.Application.Authentication;
using CustomerReturnCRM.Infrastructure.Identity;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CustomerReturnCRM.Infrastructure.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCredentials(request.Email, request.Password);

        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException(errors);
        }

        return await CreateAuthenticationResultAsync(user, cancellationToken);
    }

    public async Task<AuthenticationResult?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCredentials(request.Email, request.Password);

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return await CreateAuthenticationResultAsync(user, cancellationToken);
    }

    private async Task<AuthenticationResult> CreateAuthenticationResultAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var expirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var configuredExpirationMinutes)
            ? configuredExpirationMinutes
            : 120;
        if (expirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiration must be greater than zero.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!)
        };
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var securityToken = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);
        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

        var businesses = await _dbContext.BusinessMembers
            .AsNoTracking()
            .Where(member => member.UserId == user.Id && member.Business.IsActive)
            .OrderBy(member => member.Business.Name)
            .Select(member => new AuthenticationBusinessResult(
                member.BusinessId,
                member.Business.Name,
                member.Role))
            .ToListAsync(cancellationToken);

        return new AuthenticationResult(user.Id, user.Email!, token, expiresAt, businesses);
    }

    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.");
        }
    }
}
