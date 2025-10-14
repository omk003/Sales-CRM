using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class EmailMessage
{
    public int Id { get; set; }

    public int ThreadId { get; set; }

    public string? Body { get; set; }

    public DateTime? SentAt { get; set; }

    public string? Direction { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual EmailThread Thread { get; set; } = null!;
}
