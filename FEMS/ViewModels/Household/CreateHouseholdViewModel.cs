using System.ComponentModel.DataAnnotations;

namespace FEMS.ViewModels.Household
{
    public class CreateHouseholdViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên hộ gia đình.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên hộ gia đình phải từ 2 đến 100 ký tự.")]
        [Display(Name = "Tên hộ gia đình")]
        public string HouseholdName { get; set; } = string.Empty;
    }
}

