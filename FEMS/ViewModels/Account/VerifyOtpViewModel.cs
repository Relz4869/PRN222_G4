using System.ComponentModel.DataAnnotations;

namespace FEMS.ViewModels.Account
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã xác minh OTP.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Mã xác minh phải gồm đúng 8 chữ số.")]
        [Display(Name = "Mã xác minh OTP")]
        public string Otp { get; set; } = string.Empty;
    }
}

