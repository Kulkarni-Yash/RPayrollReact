namespace RPayroll.Repositories.Security;

public class FakeTokenGenerator
{
    public Task<string> GenerateTokenAsync()
    {
        return Task.FromResult(Guid.NewGuid().ToString("N"));
    }
}
