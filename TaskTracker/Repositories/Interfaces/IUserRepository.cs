using TaskTracker.Models;

namespace TaskTracker.Repositories.Interfaces;

public interface IUserRepository
{
    Task<AppUser?>             GetByIdAsync(int id);
    Task<AppUser?>             GetByUsernameAsync(string username);
    Task<AppUser?>             GetByEmailAsync(string email);
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<int>                  CreateAsync(AppUser user);
}
