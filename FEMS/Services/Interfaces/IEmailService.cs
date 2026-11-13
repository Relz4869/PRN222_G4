namespace FEMS.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetOtpAsync(string recipientEmail, string recipientName, string otp);
    }
}

