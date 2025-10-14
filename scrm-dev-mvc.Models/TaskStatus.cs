using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class TaskStatus
{
    public int Id { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
