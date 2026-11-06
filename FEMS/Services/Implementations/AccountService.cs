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


    }
}
