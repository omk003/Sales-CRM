using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Activity
{
    public int Id { get; set; }

    public Guid OwnerId { get; set; }

    public int? DealId { get; set; }

    public int? ContactId { get; set; } 

    public int ActivityTypeId { get; set; }

    public int SubjectId { get; set; }

    public string SubjectType { get; set; }

    public string? Notes { get; set; }

    public DateTime ActivityDate { get; set; }

    public string Status { get; set; }

    public DateTime? DueDate { get; set; }

    public virtual ActivityType? ActivityType { get; set; }

    public virtual Contact? Contact { get; set; }

    public virtual Deal? Deal { get; set; }

    public virtual User Owner { get; set; } = null!;
}
