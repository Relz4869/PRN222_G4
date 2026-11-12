using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // Find user by email
        Task<User?> GetByEmailAsync(string email);

        // Find user by ID
        Task<User?> GetByIdAsync(int userId);

        // Check whether email already exists
        Task<bool> EmailExistsAsync(string email);

        // Add new user
        Task AddAsync(User user);

        // Update existing user
        Task UpdateAsync(User user);
    }
}
