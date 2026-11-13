namespace FEMS.Models.Security
{
    public class PasswordResetOtpState
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public byte[] OtpHash { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int FailedAttempts { get; set; }
        public int ResendCount { get; set; }
        public DateTime? LastResendAtUtc { get; set; }
        public DateTime FirstRequestAtUtc { get; set; }
        public bool IsInvalidated { get; set; }
    }
}

