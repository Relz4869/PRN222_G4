using System.Security.Cryptography;
using System.Text;
using FEMS.Models.Security;
using FEMS.Models.Settings;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FEMS.Services.Implementations
{
    public class PasswordResetService : IPasswordResetService
    {
        private const int WorkFactor = 11;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;
        private readonly PasswordResetSettings _settings;
        private readonly ILogger<PasswordResetService> _logger;
        private readonly byte[] _hmacKey;

        public PasswordResetService(
            IUserRepository userRepository,
            IEmailService emailService,
            IMemoryCache memoryCache,
            IOptions<PasswordResetSettings> settingsOptions,
            ILogger<PasswordResetService> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _memoryCache = memoryCache;
            _settings = settingsOptions.Value;
            _logger = logger;

            // Lấy secret key từ cấu hình, nếu chưa có thì sử dụng fallback an toàn
            string secretKey = !string.IsNullOrWhiteSpace(_settings.HmacSecretKey)
                ? _settings.HmacSecretKey
                : "FEMS_SECURE_HMAC_SECRET_KEY_FOR_OTP_AND_RESET_AUTH_2026";
            _hmacKey = Encoding.UTF8.GetBytes(secretKey);
        }

        // ==============================================================
        // 1. YÊU CẦU GỬI MÃ OTP
        // ==============================================================
        public async Task<(bool Success, string Message)> RequestPasswordResetOtpAsync(string email, string? clientIp = null)
        {
            const string genericMessage = "Nếu địa chỉ email tồn tại trong hệ thống, hướng dẫn xác minh sẽ được gửi đến email đó.";

            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Vui lòng nhập địa chỉ email hợp lệ.");
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();

            // Kiểm tra Rate Limit theo IP (nếu có)
            if (!string.IsNullOrWhiteSpace(clientIp) && IsIpRateLimited(clientIp))
            {
                _logger.LogWarning("[RATE_LIMIT] Quá nhiều yêu cầu OTP từ IP {ClientIp}", clientIp);
                return (false, "Bạn đã gửi quá nhiều yêu cầu từ thiết bị này. Vui lòng chờ 15 phút trước khi thử lại.");
            }

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                // Bảo mật: Không tiết lộ email có tồn tại trong hệ thống hay không
                return (true, genericMessage);
            }

            // Kiểm tra trạng thái OTP hiện có trong Cache
            string cacheKey = GetOtpCacheKey(normalizedEmail);
            DateTime nowUtc = DateTime.UtcNow;

            if (_memoryCache.TryGetValue(cacheKey, out PasswordResetOtpState? existingState) && existingState != null)
            {
                // Kiểm tra Cooldown 60s
                if (existingState.LastResendAtUtc.HasValue)
                {
                    var elapsedSeconds = (nowUtc - existingState.LastResendAtUtc.Value).TotalSeconds;
                    if (elapsedSeconds < _settings.ResendCooldownSeconds)
                    {
                        int waitTime = (int)Math.Ceiling(_settings.ResendCooldownSeconds - elapsedSeconds);
                        return (false, $"Vui lòng đợi {waitTime} giây trước khi gửi lại yêu cầu.");
                    }
                }

                // Kiểm tra giới hạn 3 lần / 15 phút
                if ((nowUtc - existingState.FirstRequestAtUtc).TotalMinutes < _settings.ResendWindowMinutes)
                {
                    if (existingState.ResendCount >= _settings.MaxResendAttempts)
                    {
                        return (false, "Bạn đã vượt quá số lần yêu cầu OTP cho phép (tối đa 3 lần trong 15 phút). Vui lòng thử lại sau.");
                    }
                }
            }

            // Tạo mã OTP 8 chữ số ngẫu nhiên
            string otp = GenerateSecure8DigitOtp();

            // Tính mã băm HMAC-SHA256 cho OTP
            byte[] otpHash = ComputeOtpHmac(user.UserId, normalizedEmail, otp);

            var otpState = new PasswordResetOtpState
            {
                UserId = user.UserId,
                Email = normalizedEmail,
                OtpHash = otpHash,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddMinutes(_settings.OtpExpiryMinutes),
                FailedAttempts = 0,
                ResendCount = existingState != null ? existingState.ResendCount + 1 : 1,
                LastResendAtUtc = nowUtc,
                FirstRequestAtUtc = existingState?.FirstRequestAtUtc ?? nowUtc,
                IsInvalidated = false
            };

            // Lưu vào IMemoryCache với thời hạn 5 phút
            _memoryCache.Set(cacheKey, otpState, TimeSpan.FromMinutes(_settings.OtpExpiryMinutes));

            // Gửi email OTP qua Gmail SMTP
            bool sent = await _emailService.SendPasswordResetOtpAsync(user.Email, user.FullName, otp);
            if (!sent)
            {
                // Vô hiệu hóa OTP ngay lập tức nếu email không gửi được thành công
                _memoryCache.Remove(cacheKey);
                return (false, "Không thể gửi email xác minh lúc này. Vui lòng kiểm tra lại cấu hình hoặc thử lại sau.");
            }

            RecordIpRequest(clientIp);
            return (true, genericMessage);
        }

        // ==============================================================
        // 2. XÁC MINH MÃ OTP
        // ==============================================================
        public Task<(bool Success, string Message, string? ResetAuthToken)> VerifyOtpAsync(string email, string inputOtp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(inputOtp))
            {
                return Task.FromResult<(bool, string, string?)>((false, "Vui lòng nhập đầy đủ email và mã xác minh.", null));
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();
            string cleanOtp = inputOtp.Trim();

            if (cleanOtp.Length != _settings.OtpLength || !cleanOtp.All(char.IsDigit))
            {
                return Task.FromResult<(bool, string, string?)>((false, $"Mã xác minh phải gồm đúng {_settings.OtpLength} chữ số.", null));
            }

            string cacheKey = GetOtpCacheKey(normalizedEmail);
            if (!_memoryCache.TryGetValue(cacheKey, out PasswordResetOtpState? state) ||
                state == null ||
                state.IsInvalidated ||
                DateTime.UtcNow > state.ExpiresAtUtc)
            {
                return Task.FromResult<(bool, string, string?)>((false, "Mã xác minh không tồn tại hoặc đã hết hạn. Vui lòng yêu cầu mã mới.", null));
            }

            // Kiểm tra giới hạn nhập sai (tối đa 5 lần)
            if (state.FailedAttempts >= _settings.MaxFailedAttempts)
            {
                _memoryCache.Remove(cacheKey);
                return Task.FromResult<(bool, string, string?)>((false, "Bạn đã nhập sai mã xác minh quá số lần quy định. Vui lòng yêu cầu mã mới.", null));
            }

            // Tính toán HMAC của mã OTP người dùng nhập vào
            byte[] computedHmac = ComputeOtpHmac(state.UserId, normalizedEmail, cleanOtp);

            // So sánh an toàn Constant-Time chống Timing Attack
            bool isValid = CryptographicOperations.FixedTimeEquals(computedHmac, state.OtpHash);

            if (!isValid)
            {
                state.FailedAttempts++;
                int remaining = _settings.MaxFailedAttempts - state.FailedAttempts;

                if (remaining <= 0)
                {
                    _memoryCache.Remove(cacheKey);
                    return Task.FromResult<(bool, string, string?)>((false, "Bạn đã nhập sai mã xác minh 5 lần. Mã này đã bị vô hiệu hóa.", null));
                }

                _memoryCache.Set(cacheKey, state, state.ExpiresAtUtc - DateTime.UtcNow);
                return Task.FromResult<(bool, string, string?)>((false, $"Mã xác minh không chính xác. Bạn còn {remaining} lần thử.", null));
            }

            // OTP hợp lệ -> Hủy OTP ngay lập tức (Single-use)
            _memoryCache.Remove(cacheKey);

            // Cấp Reset Authorization Token ngẫu nhiên (32 bytes)
            string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            byte[] tokenHash = ComputeTokenHmac(rawToken);
            string tokenStorageKey = GetResetAuthCacheKey(Convert.ToBase64String(tokenHash));

            var authState = new ResetAuthorizationState
            {
                UserId = state.UserId,
                Email = normalizedEmail,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ResetTokenExpiryMinutes),
                IsUsed = false
            };

            // Lưu Reset Authorization vào IMemoryCache
            _memoryCache.Set(tokenStorageKey, authState, TimeSpan.FromMinutes(_settings.ResetTokenExpiryMinutes));

            return Task.FromResult<(bool, string, string?)>((true, "Xác minh mã OTP thành công.", rawToken));
        }

        // ==============================================================
        // 3. GỬI LẠI MÃ OTP
        // ==============================================================
        public async Task<(bool Success, string Message, int CooldownSeconds)> ResendOtpAsync(string email, string? clientIp = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Vui lòng nhập địa chỉ email hợp lệ.", 0);
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();
            string cacheKey = GetOtpCacheKey(normalizedEmail);
            DateTime nowUtc = DateTime.UtcNow;

            if (_memoryCache.TryGetValue(cacheKey, out PasswordResetOtpState? state) && state != null)
            {
                if (state.LastResendAtUtc.HasValue)
                {
                    var elapsed = (nowUtc - state.LastResendAtUtc.Value).TotalSeconds;
                    if (elapsed < _settings.ResendCooldownSeconds)
                    {
                        int waitTime = (int)Math.Ceiling(_settings.ResendCooldownSeconds - elapsed);
                        return (false, $"Vui lòng đợi {waitTime} giây trước khi gửi lại mã.", waitTime);
                    }
                }

                if ((nowUtc - state.FirstRequestAtUtc).TotalMinutes < _settings.ResendWindowMinutes &&
                    state.ResendCount >= _settings.MaxResendAttempts)
                {
                    return (false, "Bạn đã đạt giới hạn gửi lại mã (tối đa 3 lần trong 15 phút). Vui lòng thử lại sau.", 0);
                }
            }

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                return (true, "Mã xác minh mới đã được gửi nếu email tồn tại trong hệ thống.", _settings.ResendCooldownSeconds);
            }

            // Tạo OTP mới và vô hiệu hóa OTP cũ
            string newOtp = GenerateSecure8DigitOtp();
            byte[] newHash = ComputeOtpHmac(user.UserId, normalizedEmail, newOtp);

            var newState = new PasswordResetOtpState
            {
                UserId = user.UserId,
                Email = normalizedEmail,
                OtpHash = newHash,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddMinutes(_settings.OtpExpiryMinutes),
                FailedAttempts = 0,
                ResendCount = (state?.ResendCount ?? 0) + 1,
                LastResendAtUtc = nowUtc,
                FirstRequestAtUtc = state?.FirstRequestAtUtc ?? nowUtc,
                IsInvalidated = false
            };

            _memoryCache.Set(cacheKey, newState, TimeSpan.FromMinutes(_settings.OtpExpiryMinutes));

            bool sent = await _emailService.SendPasswordResetOtpAsync(user.Email, user.FullName, newOtp);
            if (!sent)
            {
                _memoryCache.Remove(cacheKey);
                return (false, "Không thể gửi email xác minh lúc này. Vui lòng thử lại sau.", 0);
            }

            return (true, "Mã xác minh mới đã được gửi tới email của bạn.", _settings.ResendCooldownSeconds);
        }

        // ==============================================================
        // 4. KIỂM TRA RESET AUTHORIZATION TOKEN
        // ==============================================================
        public Task<(bool Valid, int UserId, string? Email)> ValidateResetAuthorizationAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult<(bool, int, string?)>((false, 0, null));
            }

            byte[] tokenHash = ComputeTokenHmac(token);
            string tokenStorageKey = GetResetAuthCacheKey(Convert.ToBase64String(tokenHash));

            if (_memoryCache.TryGetValue(tokenStorageKey, out ResetAuthorizationState? state) &&
                state != null &&
                !state.IsUsed &&
                DateTime.UtcNow <= state.ExpiresAtUtc)
            {
                return Task.FromResult<(bool, int, string?)>((true, state.UserId, state.Email));
            }

            return Task.FromResult<(bool, int, string?)>((false, 0, null));
        }

        // ==============================================================
        // 5. ĐẶT LẠI MẬT KHẨU
        // ==============================================================
        public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
        {
            var auth = await ValidateResetAuthorizationAsync(token);
            if (!auth.Valid || auth.UserId <= 0)
            {
                return (false, "Phiên đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thực hiện lại từ đầu.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8 || newPassword.Length > 15)
            {
                return (false, "Mật khẩu mới phải từ 8 đến 15 ký tự.");
            }

            var user = await _userRepository.GetByIdAsync(auth.UserId);
            if (user == null)
            {
                return (false, "Không tìm thấy thông tin tài khoản người dùng.");
            }

            // Băm mật khẩu mới bằng BCrypt (Work Factor: 11)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, WorkFactor);
            await _userRepository.UpdateAsync(user);

            // Vô hiệu hóa Reset Authorization Token ngay sau khi hoàn thành
            byte[] tokenHash = ComputeTokenHmac(token);
            string tokenStorageKey = GetResetAuthCacheKey(Convert.ToBase64String(tokenHash));
            _memoryCache.Remove(tokenStorageKey);

            _logger.LogInformation("[PASSWORD_RESET] Đã đặt lại mật khẩu thành công cho UserID {UserId}", user.UserId);
            return (true, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.");
        }

        // ==============================================================
        // HELPER FUNCTIONS
        // ==============================================================

        private string GenerateSecure8DigitOtp()
        {
            return RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");
        }

        private byte[] ComputeOtpHmac(int userId, string email, string otp)
        {
            string payload = $"PURPOSE=PASSWORD_RESET;USERID={userId};EMAIL={email};OTP={otp}";
            using var hmac = new HMACSHA256(_hmacKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }

        private byte[] ComputeTokenHmac(string token)
        {
            string payload = $"PURPOSE=RESET_AUTH_TOKEN;TOKEN={token}";
            using var hmac = new HMACSHA256(_hmacKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }

        private static string GetOtpCacheKey(string email) => $"fems:pwd_reset_otp:{email}";
        private static string GetResetAuthCacheKey(string hash) => $"fems:pwd_reset_auth:{hash}";
        private static string GetIpRateLimitKey(string ip) => $"fems:rate_limit_ip:{ip}";

        private bool IsIpRateLimited(string ip)
        {
            string key = GetIpRateLimitKey(ip);
            if (_memoryCache.TryGetValue(key, out int count) && count >= 10)
            {
                return true;
            }
            return false;
        }

        private void RecordIpRequest(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            string key = GetIpRateLimitKey(ip);
            int current = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                return 0;
            });
            _memoryCache.Set(key, current + 1, TimeSpan.FromMinutes(15));
        }
    }
}

