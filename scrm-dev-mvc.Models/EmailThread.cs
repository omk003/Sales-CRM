using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class EmailThread
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int ContactId { get; set; }

    public string Subject { get; set; }

    public bool? IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public virtual ICollection<EmailMessage> EmailMessages { get; set; } = new List<EmailMessage>();

    public virtual User User { get; set; } = null!;
}
