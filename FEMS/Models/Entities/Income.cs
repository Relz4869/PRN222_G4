using System;
using System.Collections.Generic;

namespace FEMS.Models.Entities;

public partial class Income
{
    public int IncomeId { get; set; }

    public int HouseholdMemberId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly IncomeDate { get; set; }

    public string? Description { get; set; }

    public int? WalletId { get; set; }

    public virtual HouseholdMember HouseholdMember { get; set; } = null!;

    public virtual Wallet? Wallet { get; set; }
}
