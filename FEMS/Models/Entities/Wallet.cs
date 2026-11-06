using System;
using System.Collections.Generic;

namespace FEMS.Models.Entities;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int HouseholdMemberId { get; set; }

    public string WalletName { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal? LimitAmount { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual HouseholdMember HouseholdMember { get; set; } = null!;

    public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();
}
