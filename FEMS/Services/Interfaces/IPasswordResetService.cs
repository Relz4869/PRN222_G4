namespace FEMS.Services.Interfaces
{
    public interface IPasswordResetService
    {
        Task<(bool Success, string Message)> RequestPasswordResetOtpAsync(string email, string? clientIp = null);

        Task<(bool Success, string Message, string? ResetAuthToken)> VerifyOtpAsync(string email, string inputOtp);

        Task<(bool Success, string Message, int CooldownSeconds)> ResendOtpAsync(string email, string? clientIp = null);

        Task<(bool Valid, int UserId, string? Email)> ValidateResetAuthorizationAsync(string token);

        Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword);
    }
}

