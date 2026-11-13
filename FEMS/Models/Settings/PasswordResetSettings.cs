namespace FEMS.Models.Settings
{
    public class PasswordResetSettings
    {
        public string HmacSecretKey { get; set; } = string.Empty;
        public int OtpLength { get; set; } = 8;
        public int OtpExpiryMinutes { get; set; } = 5;
        public int ResendCooldownSeconds { get; set; } = 60;
        public int MaxFailedAttempts { get; set; } = 5;
        public int MaxResendAttempts { get; set; } = 3;
        public int ResendWindowMinutes { get; set; } = 15;
        public int ResetTokenExpiryMinutes { get; set; } = 10;
    }
}

