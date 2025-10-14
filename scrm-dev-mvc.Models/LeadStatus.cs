using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class LeadStatus
{
    public int Id { get; set; }

    public string LeadStatusName { get; set; } = null!;

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
