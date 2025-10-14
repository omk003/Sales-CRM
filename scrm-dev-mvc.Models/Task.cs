using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Task
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int PriorityId { get; set; }

    public int StatusId { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Priority Priority { get; set; } = null!;

    public virtual TaskStatus Status { get; set; } = null!;
}
