using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Stage
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();
}
