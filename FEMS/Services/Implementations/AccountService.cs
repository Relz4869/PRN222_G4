using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;

namespace FEMS.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private const int WorkFactor = 11;
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

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return null;
            }

            // Verify password using BCrypt
            try
            {
                bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                return isValid ? user : null;
            }
            catch
            {
                // Phòng ngừa lỗi nếu hash trong DB chưa hợp lệ (ví dụ trước khi migrate)
                return null;
            }
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

            // Hash password with BCrypt (Work Factor: 11)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

            // Create new User
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHash,
                Phone = string.IsNullOrWhiteSpace(phone)
                    ? null
                    : phone.Trim()
            };

            // Save to database
            await _userRepository.AddAsync(user);

            return true;
        }

        // =========================================
        // CHANGE PASSWORD
        // =========================================

        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            if (userId <= 0)
            {
                return (false, "Người dùng không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "Vui lòng nhập đầy đủ thông tin mật khẩu.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "Không tìm thấy thông tin tài khoản.");
            }

            // 1. Kiểm tra mật khẩu hiện tại bằng BCrypt.Verify
            bool isCurrentValid = false;
            try
            {
                isCurrentValid = !string.IsNullOrEmpty(user.PasswordHash) &&
                                 BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            }
            catch
            {
                isCurrentValid = false;
            }

            if (!isCurrentValid)
            {
                return (false, "Mật khẩu hiện tại không chính xác.");
            }

            // 2. Kiểm tra mật khẩu mới không được trùng với mật khẩu hiện tại
            bool isSamePassword = false;
            try
            {
                isSamePassword = BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash);
            }
            catch
            {
                isSamePassword = false;
            }

            if (isSamePassword)
            {
                return (false, "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            // 3. Băm mật khẩu mới bằng BCrypt và lưu vào Database
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, WorkFactor);
            await _userRepository.UpdateAsync(user);

            return (true, "Đổi mật khẩu thành công.");
        }
    }
}
