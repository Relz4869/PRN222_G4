using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;

namespace FEMS.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;

        public AccountService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // =========================================
        // LOGIN
        // =========================================

        public async Task<User?> AuthenticateAsync(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(email.Trim());

            if (user == null)
            {
                return null;
            }

            // Compare plain text password
            return user.Password == password ? user : null;
        }
        // =========================================
        // REGISTER
        // =========================================
        public async Task<bool> RegisterAsync(
            string fullName,
            string email,
            string password,
            string? phone)
        {
            // Validate required information
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            // Normalize input
            email = email.Trim();
            fullName = fullName.Trim();

            // Check duplicate email
            bool emailExists = await _userRepository.EmailExistsAsync(email);

            if (emailExists)
            {
                return false;
            }

            // Create new User
            var user = new User
            {
                FullName = fullName,
                Email = email,
                Password = password,
                Phone = string.IsNullOrWhiteSpace(phone)
                    ? null
                    : phone.Trim()
            };

            // Save to database
            await _userRepository.AddAsync(user);

            return true;
        }


    }
}
