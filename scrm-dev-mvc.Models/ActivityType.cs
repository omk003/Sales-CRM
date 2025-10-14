using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class ActivityType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
