using System;
using System.Collections.Generic;

namespace FEMS.Models.Entities;

public partial class Household
{
    public int HouseholdId { get; set; }

    public string HouseholdName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
}
