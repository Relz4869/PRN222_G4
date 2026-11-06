using System;
using System.Collections.Generic;

namespace FEMS.Models.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int HouseholdMemberId { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual HouseholdMember HouseholdMember { get; set; } = null!;
}
