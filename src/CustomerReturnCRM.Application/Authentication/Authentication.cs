namespace CustomerReturnCRM.Application.Authentication;

public sealed class RegisterRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public sealed class LoginRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public sealed record AuthenticationBusinessResult(Guid Id, string Name, string Role);

public sealed record AuthenticationResult(
    Guid UserId,
    string Email,
    string Token,
    DateTime ExpiresAt,
    IReadOnlyList<AuthenticationBusinessResult> Businesses);

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
