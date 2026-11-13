namespace FEMS.Models.Security
{
    public class ResetAuthorizationState
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public bool IsUsed { get; set; }
    }
}

