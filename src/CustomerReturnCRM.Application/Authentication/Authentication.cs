namespace CustomerReturnCRM.Application.Authentication;

public sealed class RegisterRequest
{
    public string Mobile { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public sealed class LoginRequest
{
    public string Mobile { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public sealed class ForgotPasswordRequest
{
    public string Mobile { get; init; } = null!;
}

public sealed class VerifyOtpRequest
{
    public string Mobile { get; init; } = null!;
    public string Otp { get; init; } = null!;
}

public sealed class ResetPasswordRequest
{
    public string Mobile { get; init; } = null!;
    public string Otp { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
}

public sealed record AuthenticationBusinessResult(Guid Id, string Name, string Role, string? PublicSlug);

public sealed record AuthenticationResult(
    Guid UserId,
    string Mobile,
    string Token,
    DateTime ExpiresAt,
    IReadOnlyList<AuthenticationBusinessResult> Businesses);

public sealed record OtpVerificationResult(bool Verified);

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<OtpVerificationResult> VerifyPasswordResetOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
