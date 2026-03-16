using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Repositories.Security;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTokenGenerator _tokenGenerator;

    public AuthService(IUnitOfWork unitOfWork, FakeTokenGenerator tokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(username, includeInactive: true);
        if (user == null)
        {
            return null;
        }

        if (!string.Equals(user.Password, password, StringComparison.Ordinal))
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

        return user;
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
