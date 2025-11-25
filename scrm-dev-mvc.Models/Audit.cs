using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Audit
{
    public int Id { get; set; }

    public Guid OwnerId { get; set; }

    public int? RecordId { get; set; }

    public string? TableName { get; set; }

    public string? FieldName { get; set; }

    public string? OldValue { get; set; }
     
    public string? NewValue { get; set; }

    public DateTime? Timestamp { get; set; }

    public virtual User Owner { get; set; } = null!;
}
