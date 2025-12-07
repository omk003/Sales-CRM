using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Call
{
    public int Id { get; set; }

    public string Sid { get; set; }

    public string? Notes { get; set; }

    public Guid UserId { get; set; }

    public int ContactId { get; set; }

    public int? DurationSeconds { get; set; }

    public string Outcome { get; set; }

    public string Direction { get; set; }

    public DateTime CallTime { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
