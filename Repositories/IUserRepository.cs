using UserAuthApi.Models;

namespace UserAuthApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}