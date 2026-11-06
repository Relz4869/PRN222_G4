using System;
using System.Collections.Generic;

namespace FEMS.Models.Entities;

public partial class Expense
{
    public int ExpenseId { get; set; }

    public int HouseholdMemberId { get; set; }

    public int CategoryId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public string? Description { get; set; }

    public int? WalletId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual HouseholdMember HouseholdMember { get; set; } = null!;

    public virtual Wallet? Wallet { get; set; }
}
