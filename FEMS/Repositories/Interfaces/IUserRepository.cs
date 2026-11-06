using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // Find user by email
        Task<User?> GetByEmailAsync(string email);

        // Check whether email already exists
        Task<bool> EmailExistsAsync(string email);

        // Add new user
        Task AddAsync(User user);
    }
}
