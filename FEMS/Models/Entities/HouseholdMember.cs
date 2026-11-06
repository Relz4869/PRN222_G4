using System;
using System.Collections.Generic;

namespace FEMS.Models.Entities;

public partial class HouseholdMember
{
    public int HouseholdMemberId { get; set; }

    public int HouseholdId { get; set; }

    public int UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public decimal? AlertAmount { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual Household Household { get; set; } = null!;

    public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
