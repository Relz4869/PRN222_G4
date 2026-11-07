using System.ComponentModel.DataAnnotations;

namespace FEMS.ViewModels.Category
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = "Active";

        public int HouseholdMemberId { get; set; }

        // Danh sách hiển thị bên bảng phải
        public List<Models.Entities.Category> Categories { get; set; } = new List<Models.Entities.Category>();
    }
}
