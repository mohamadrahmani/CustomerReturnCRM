using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CustomerReturnCRM.Application.Authentication;
using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Infrastructure.Identity;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CustomerReturnCRM.Infrastructure.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private const int OtpLifetimeMinutes = 10;
    private const int OtpRequestCooldownSeconds = 60;
    private static readonly Regex MobileRegex = new(@"^\+?[0-9]{8,15}$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"^[A-Za-z0-9]{8,}$", RegexOptions.Compiled);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _smsService;
    private readonly TimeProvider _timeProvider;

    public AuthenticationService(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IConfiguration configuration, ISmsService smsService, TimeProvider timeProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _smsService = smsService;
        _timeProvider = timeProvider;
    }

    public async Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var mobile = NormalizeMobile(request.Mobile);
        ValidatePassword(request.Password);

        var existing = await _userManager.Users.AnyAsync(x => x.PhoneNumber == mobile, cancellationToken);
        if (existing) throw new InvalidOperationException("این شماره موبایل قبلاً ثبت شده است.");

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = mobile, PhoneNumber = mobile, PhoneNumberConfirmed = false };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
        return await CreateAuthenticationResultAsync(user, cancellationToken);
    }

    public async Task<AuthenticationResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var mobile = NormalizeMobile(request.Mobile);
        ValidatePassword(request.Password);
        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.PhoneNumber == mobile, cancellationToken);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password)) return null;
        return await CreateAuthenticationResultAsync(user, cancellationToken);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var mobile = NormalizeMobile(request.Mobile);
        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.PhoneNumber == mobile, cancellationToken);
        if (user is null) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (user.PasswordResetOtpRequestedAtUtc.HasValue && (now - user.PasswordResetOtpRequestedAtUtc.Value).TotalSeconds < OtpRequestCooldownSeconds)
            throw new InvalidOperationException("لطفاً کمی بعد دوباره درخواست کد کنید.");

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.PasswordResetOtpHash = HashOtp(otp);
        user.PasswordResetOtpExpiresAtUtc = now.AddMinutes(OtpLifetimeMinutes);
        user.PasswordResetOtpRequestedAtUtc = now;
        user.PasswordResetOtpAttemptCount = 0;
        await _userManager.UpdateAsync(user);

        var patternId = _configuration["Sms:SmsIr:PasswordResetPatternId"];
        if (string.IsNullOrWhiteSpace(patternId))
            throw new InvalidOperationException("SMS.ir password reset pattern is not configured.");

        var smsResult = await _smsService.SendPatternAsync(
            new SmsPatternRequest(mobile, patternId, new Dictionary<string, string> { ["code"] = otp }),
            cancellationToken);
        if (smsResult.Items.All(x => !x.Accepted))
            throw new InvalidOperationException("ارسال کد تأیید انجام نشد.");
    }

    public async Task<OtpVerificationResult> VerifyPasswordResetOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetOtpUserAsync(request.Mobile, request.Otp, cancellationToken);
        return new OtpVerificationResult(user is not null);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.NewPassword);
        var user = await GetOtpUserAsync(request.Mobile, request.Otp, cancellationToken) ?? throw new ArgumentException("کد تأیید نامعتبر یا منقضی شده است.");
        var result = await _userManager.RemovePasswordAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException(errors);
        }
        result = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException(errors);
        }
        user.PasswordResetOtpHash = null;
        user.PasswordResetOtpExpiresAtUtc = null;
        user.PasswordResetOtpRequestedAtUtc = null;
        user.PasswordResetOtpAttemptCount = 0;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
    }

    private async Task<ApplicationUser?> GetOtpUserAsync(string mobileInput, string otp, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(mobileInput);
        if (!Regex.IsMatch(otp ?? "", @"^[0-9]{6}$")) return null;
        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.PhoneNumber == mobile, cancellationToken);
        if (user?.PasswordResetOtpHash is null || user.PasswordResetOtpExpiresAtUtc <= _timeProvider.GetUtcNow().UtcDateTime) return null;
        if (user.PasswordResetOtpAttemptCount >= 5) return null;
        var valid = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(user.PasswordResetOtpHash),
            Convert.FromHexString(HashOtp(otp)));
        if (!valid)
        {
            user.PasswordResetOtpAttemptCount++;
            if (user.PasswordResetOtpAttemptCount >= 5)
            {
                user.PasswordResetOtpHash = null;
                user.PasswordResetOtpExpiresAtUtc = null;
            }
            await _userManager.UpdateAsync(user);
            return null;
        }
        return user;
    }

    private static string HashOtp(string otp) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));

    private static string NormalizeMobile(string mobile)
    {
        var value = (mobile ?? "").Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (!MobileRegex.IsMatch(value)) throw new ArgumentException("شماره موبایل نامعتبر است.");
        return value;
    }

    private static void ValidatePassword(string password)
    {
        if (!PasswordRegex.IsMatch(password ?? "")) throw new ArgumentException("رمز عبور باید حداقل ۸ کاراکتر و فقط شامل حروف انگلیسی و اعداد باشد.");
    }

    private async Task<AuthenticationResult> CreateAuthenticationResultAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var expirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var configuredExpirationMinutes) ? configuredExpirationMinutes : 120;
        if (expirationMinutes <= 0) throw new InvalidOperationException("JWT expiration must be greater than zero.");
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.MobilePhone, user.PhoneNumber!), new Claim("mobile", user.PhoneNumber!) };
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)));
        var businesses = await _dbContext.BusinessMembers.AsNoTracking().Where(x => x.UserId == user.Id && x.Business.IsActive).OrderBy(x => x.Business.Name).Select(x => new AuthenticationBusinessResult(x.BusinessId, x.Business.Name, x.Role)).ToListAsync(cancellationToken);
        return new AuthenticationResult(user.Id, user.PhoneNumber!, token, expiresAt, businesses);
    }
}
