using FEMS.Data;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly FemsDbContext _context;

        public UserRepository(FemsDbContext context)
        {
            _context = context;
        }

        // Find user by email
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // Find user by ID
        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Set<User>()
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        // Check whether email already exists
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Set<User>()
                .AnyAsync(u => u.Email == email);
        }

        // Add new user
        public async Task AddAsync(User user)
        {
            await _context.Set<User>().AddAsync(user);
            await _context.SaveChangesAsync();
        }

        // Update existing user
        public async Task UpdateAsync(User user)
        {
            _context.Set<User>().Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
