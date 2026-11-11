using System.ComponentModel.DataAnnotations;

namespace FEMS.ViewModels.Transaction
{
    public class OwnTransactionItemViewModel
    {
        public int TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Expense" | "Income"
        public DateOnly TransactionDate { get; set; }
        public int? WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public string CategoryDisplay { get; set; } = string.Empty;
        public bool IsCategoryDeleted { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthFilterOption
    {
        public string Value { get; set; } = string.Empty; // "yyyy-MM"
        public string Label { get; set; } = string.Empty; // "Tháng MM/yyyy"
    }

    public class TransactionManagementViewModel
    {
        public int HouseholdMemberId { get; set; }
        public bool HasHouseholdMembership { get; set; }

        // =========================================
        // 1. DỮ LIỆU FORM GHI NHẬN GIAO DỊCH MỚI (Create Transaction)
        // =========================================
        [Required]
        public string TransactionType { get; set; } = "Expense"; // "Expense" hoặc "Income"

        [Required(ErrorMessage = "Vui lòng chọn ví cá nhân.")]
        public int WalletId { get; set; }

        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền giao dịch.")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giao dịch phải lớn hơn 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày giao dịch.")]
        public DateOnly TransactionDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        // =========================================
        // 2. DANH SÁCH DROPDOWN (Ví cá nhân & Danh mục Active)
        // =========================================
        public List<FEMS.Models.Entities.Wallet> Wallets { get; set; } = new();
        public List<FEMS.Models.Entities.Category> ActiveCategories { get; set; } = new();

        // =========================================
        // 3. BỘ LỌC & LỊCH SỬ GIAO DỊCH CÁ NHÂN (View Own Transactions)
        // =========================================
        public int? FilterWalletId { get; set; }
        public string? FilterMonth { get; set; } // Định dạng "yyyy-MM" hoặc rỗng (Tất cả tháng)
        public List<MonthFilterOption> AvailableMonths { get; set; } = new();

        public List<OwnTransactionItemViewModel> Transactions { get; set; } = new();
    }
}

