using Microsoft.AspNetCore.Identity;

namespace CustomerReturnCRM.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? PasswordResetOtpHash { get; set; }
    public DateTime? PasswordResetOtpExpiresAtUtc { get; set; }
    public DateTime? PasswordResetOtpRequestedAtUtc { get; set; }
    public int PasswordResetOtpAttemptCount { get; set; }
}
