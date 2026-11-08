using FEMS.Models.Entities;

namespace FEMS.Services.Interfaces
{
    public interface IAccountService
    {
        // Authenticate user
        Task<User?> AuthenticateAsync(string email, string password);

        Task<bool> RegisterAsync(
            string fullName,
            string email,
            string password,
            string? phone
        );
    }
}
