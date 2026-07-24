using PassDo.Application.Common.Interfaces;

namespace PassDo.Infrastructure.Identity;

public class PasswordHasherService : IPasswordHasher
{
    public string Hash(string password) => PasswordHasher.Hash(password);

    public bool Verify(string password, string storedHash) => PasswordHasher.Verify(password, storedHash);
}
