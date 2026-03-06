using RPayroll.Domain.DTOs.Auth;

namespace RPayroll.Domain.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task LogoutAsync(int userId);
}
