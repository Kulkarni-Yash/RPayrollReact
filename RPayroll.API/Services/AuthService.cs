using RPayroll.Domain.DTOs.Auth;
using RPayroll.Domain.Enums;
using RPayroll.Domain.Interfaces.Services;
using RPayroll.Infrastructure.Security;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTokenGenerator _tokenGenerator;

    public AuthService(IUnitOfWork unitOfWork, FakeTokenGenerator tokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username, includeInactive: true);
        if (user == null)
        {
            return null;
        }

        if (!string.Equals(user.Password, request.Password, StringComparison.Ordinal))
        {
            return null;
        }

        if (user.Status != StatusCode.Accepted)
        {
            return null;
        }

        var token = await _tokenGenerator.GenerateTokenAsync();
        user.Token = token;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role?.Name ?? string.Empty
        };
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, includeInactive: true);
        if (user == null)
        {
            return;
        }

        user.Token = null;
        user.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
}
